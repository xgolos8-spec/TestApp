using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Dapper;
using FluentValidation;
using Npgsql;
using TestApp.Models;

namespace TestApp.Services;

public interface IParseService
{
    Task<ParseResponse> ProcessAsync(ParseRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Бизнес-логика: Base64, AngleSharp, email (source-generated regex), AES-256 ECB, Dapper.
/// </summary>
public sealed partial class ParseService(
    IValidator<ParseRequest> validator,
    IConfiguration configuration,
    ILogger<ParseService> logger) : IParseService
{
    private static readonly HtmlParser HtmlParser = new();

    private readonly string _connectionString = configuration.GetConnectionString("Db")
        ?? throw new InvalidOperationException("Connection string 'Db' is not configured.");

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    public async Task<ParseResponse> ProcessAsync(ParseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return ParseResponse.Fail(
                    ErrorCodes.ValidationError,
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
            }

            if (!TryDecodeBase64Utf8(request.UrlB64!, out var url, out var urlDecodeError))
            {
                return ParseResponse.Fail(ErrorCodes.UrlBase64DecodeError, urlDecodeError);
            }

            if (!TryDecodeBase64Utf8(request.PageB64!, out var page, out var pageDecodeError))
            {
                return ParseResponse.Fail(ErrorCodes.PageBase64DecodeError, pageDecodeError);
            }

            string keyError = string.Empty;
            string cipherError = string.Empty;

            if (!TryDecodeBase64(request.KeyBytesB64!, out var keyBytes, out keyError) ||
                !TryDecodeBase64(request.EncryptedTextBytesB64!, out var cipherBytes, out cipherError))
            {
                return ParseResponse.Fail(
                    ErrorCodes.CryptoBase64DecodeError,
                    string.IsNullOrWhiteSpace(keyError) ? cipherError : $"{keyError}; {cipherError}");
            }

            using var document = await HtmlParser.ParseDocumentAsync(page, cancellationToken);

            IElement[] elements;
            try
            {
                elements = document.QuerySelectorAll(request.Selector!).ToArray();
            }
            catch (Exception ex)
            {
                return ParseResponse.Fail(ErrorCodes.SelectorError, ex.Message);
            }

            var attribute = request.Attribute!;
            var attributes = elements
                .Select(e => e.GetAttribute(attribute) ?? string.Empty)
                .ToList();

            var emails = EmailRegex().Matches(page)
                .Select(match => match.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string plainText;
            try
            {
                plainText = DecryptAes256Ecb(cipherBytes, keyBytes);
            }
            catch (Exception ex)
            {
                return ParseResponse.Fail(ErrorCodes.AesDecryptError, ex.Message);
            }

            await SaveElementsAsync(elements, attribute, cancellationToken);

            return new ParseResponse
            {
                Url = url,
                ElementsCount = elements.Length,
                ElementsAttrList = attributes,
                EmailsCount = emails.Count,
                EmailsList = emails,
                DecryptedPlainText = plainText
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while processing parse request.");
            return ParseResponse.Fail(ErrorCodes.InternalError, "An unexpected error occurred.");
        }
    }

    private async Task SaveElementsAsync(
        IReadOnlyList<IElement> elements,
        string attribute,
        CancellationToken cancellationToken)
    {
        if (elements.Count == 0)
        {
            return;
        }

        var attrValues = elements.Select(e => e.GetAttribute(attribute) ?? string.Empty).ToArray();
        var htmls = elements.Select(e => e.OuterHtml).ToArray();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql =
            """
            INSERT INTO elements (attr_value, element_html)
            SELECT x.attr_value, x.element_html
            FROM unnest(@AttrValues, @ElementHtmls) AS x(attr_value, element_html)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { AttrValues = attrValues, ElementHtmls = htmls },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }

    private static string DecryptAes256Ecb(byte[] cipherBytes, byte[] keyBytes)
    {
        if (keyBytes.Length != 32)
        {
            throw new CryptographicException($"AES-256 key must be exactly 32 bytes, got {keyBytes.Length}.");
        }

        if (cipherBytes.Length == 0 || cipherBytes.Length % 16 != 0)
        {
            throw new CryptographicException("Ciphertext length must be a non-zero multiple of the AES block size (16).");
        }

#pragma warning disable CA5358 // ECB is required by the assignment
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = keyBytes;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
#pragma warning restore CA5358

        return Encoding.UTF8.GetString(plainBytes).TrimEnd('\0');
    }

    private static bool TryDecodeBase64(string value, out byte[] result, out string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = Array.Empty<byte>();
            error = "Value is empty.";
            return false;
        }

        try
        {
            result = Convert.FromBase64String(value);
            error = string.Empty;
            return true;
        }
        catch (FormatException ex)
        {
            result = Array.Empty<byte>();
            error = ex.Message;
            return false;
        }
    }

    private static bool TryDecodeBase64Utf8(string value, out string result, out string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = string.Empty;
            error = "Value is empty.";
            return false;
        }

        try
        {
            result = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            error = string.Empty;
            return true;
        }
        catch (FormatException ex)
        {
            result = string.Empty;
            error = ex.Message;
            return false;
        }
        catch (DecoderFallbackException ex)
        {
            result = string.Empty;
            error = ex.Message;
            return false;
        }
    }
}
