using FluentValidation;
using MemberService.Application.DTOs.Auth;

namespace MemberService.Application.Validators;

/// <summary>
/// Validator for LoginRequest using FluentValidation.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
