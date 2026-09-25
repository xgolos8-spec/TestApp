using FluentValidation;

namespace TestApp.Models;

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