using FluentValidation;
using MemberService.Application.DTOs.Users;

namespace MemberService.Application.Validators;

/// <summary>
/// Validator for ChangePasswordRequest
/// Validates that both old and new passwords are provided and meet requirements
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty()
            .WithMessage("Old password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .Length(8, 128)
            .WithMessage("New password must be between 8 and 128 characters");
    }
}
