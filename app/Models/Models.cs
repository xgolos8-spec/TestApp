using System.Text.Json.Serialization;
using FluentValidation;

namespace TestApp.Models;

public static class ErrorCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string UrlBase64DecodeError = "URL_BASE64_DECODE_ERROR";
    public const string PageBase64DecodeError = "PAGE_BASE64_DECODE_ERROR";
    public const string CryptoBase64DecodeError = "CRYPTO_BASE64_DECODE_ERROR";
    public const string SelectorError = "SELECTOR_ERROR";
    public const string AesDecryptError = "AES_DECRYPT_ERROR";
    public const string InternalError = "INTERNAL_ERROR";
}

public sealed class ParseRequest
{
    [JsonPropertyName("selector")]
    public string? Selector { get; set; }

    [JsonPropertyName("attribute")]
    public string? Attribute { get; set; }

    [JsonPropertyName("url_b64")]
    public string? UrlB64 { get; set; }

    [JsonPropertyName("encrypted_text_bytes_b64")]
    public string? EncryptedTextBytesB64 { get; set; }

    [JsonPropertyName("key_bytes_b64")]
    public string? KeyBytesB64 { get; set; }

    [JsonPropertyName("page_b64")]
    public string? PageB64 { get; set; }
}

public sealed class ParseResponse
{
    [JsonPropertyName("is_error")]
    public int IsError { get; set; }

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("elements_count")]
    public int ElementsCount { get; set; }

    [JsonPropertyName("emails_count")]
    public int EmailsCount { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("decrypted_plain_text")]
    public string DecryptedPlainText { get; set; } = string.Empty;

    [JsonPropertyName("elements_attr_list")]
    public List<string> ElementsAttrList { get; set; } = [];

    [JsonPropertyName("emails_list")]
    public List<string> EmailsList { get; set; } = [];

    public static ParseResponse Fail(string code, string message) => new()
    {
        IsError = 1,
        ErrorCode = string.IsNullOrWhiteSpace(code) ? ErrorCodes.InternalError : code,
        ErrorMessage = message
    };
}

public sealed class ParseRequestValidator : AbstractValidator<ParseRequest>
{
    public ParseRequestValidator()
    {
        RuleFor(x => x.Selector)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("selector is required")
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("selector must not be empty");

        RuleFor(x => x.Attribute)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("attribute is required")
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("attribute must not be empty");

        RuleFor(x => x.UrlB64)
            .NotEmpty().WithMessage("url_b64 is required")
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("url_b64 must not be empty");

        RuleFor(x => x.EncryptedTextBytesB64)
            .NotEmpty().WithMessage("encrypted_text_bytes_b64 is required")
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("encrypted_text_bytes_b64 must not be empty");

        RuleFor(x => x.KeyBytesB64)
            .NotEmpty().WithMessage("key_bytes_b64 is required")
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("key_bytes_b64 must not be empty");

        RuleFor(x => x.PageB64)
            .NotEmpty().WithMessage("page_b64 is required")
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("page_b64 must not be empty");
    }
}
