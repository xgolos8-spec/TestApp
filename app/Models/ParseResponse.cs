using System.Text.Json.Serialization;

namespace TestApp.Models;

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