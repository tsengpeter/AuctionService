using FluentValidation;
using MemberService.Application.DTOs.Users;

namespace MemberService.Application.Validators;

/// <summary>
/// Validator for UpdateProfileRequest
/// Validates optional email format and username format/length
/// </summary>
public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        // Email is optional, but if provided must be valid format
        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Email address format is invalid");
        });

        // Username is optional, but if provided must follow format rules
        When(x => !string.IsNullOrWhiteSpace(x.Username), () =>
        {
            RuleFor(x => x.Username)
                .Length(3, 50)
                .WithMessage("Username must be between 3 and 50 characters")
                .Matches(@"^[a-zA-Z\u4E00-\u9FA5\s]+$")
                .WithMessage("Username can only contain letters (Latin, Chinese, etc) and spaces");
        });
    }
}
