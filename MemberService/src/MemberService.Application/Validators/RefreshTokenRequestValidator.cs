using FluentValidation;
using MemberService.Application.DTOs.Auth;

namespace MemberService.Application.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .MinimumLength(10).WithMessage("Refresh token must be at least 10 characters long");
    }
}