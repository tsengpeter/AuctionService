using FluentValidation;
using MemberService.Application.DTOs.Users;

namespace MemberService.Application.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Username), () =>
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username cannot be empty.")
                .MinimumLength(3)
                .WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(50)
                .WithMessage("Username must not exceed 50 characters.")
                .Matches(@"^[\p{L}\s]+$")
                .WithMessage("Username can only contain letters and spaces.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email cannot be empty.")
                .EmailAddress()
                .WithMessage("Email must be a valid email address.")
                .MaximumLength(255)
                .WithMessage("Email must not exceed 255 characters.");
        });
    }
}