using FluentValidation;
using MemberService.Application.DTOs.Auth;

namespace MemberService.Application.Validators;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Verification code is required")
            .Length(6)
            .WithMessage("Verification code must be 6 digits")
            .Matches(@"^\d{6}$")
            .WithMessage("Verification code must contain only digits");
    }
}

public class VerifyPhoneRequestValidator : AbstractValidator<VerifyPhoneRequest>
{
    public VerifyPhoneRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Verification code is required")
            .Length(6)
            .WithMessage("Verification code must be 6 digits")
            .Matches(@"^\d{6}$")
            .WithMessage("Verification code must contain only digits");
    }
}
