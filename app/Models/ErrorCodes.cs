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