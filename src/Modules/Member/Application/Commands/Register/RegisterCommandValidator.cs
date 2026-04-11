using FluentValidation;

namespace Member.Application.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(30);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.PhoneCountryCodeId)
            .GreaterThan(0).WithMessage("PhoneCountryCodeId must be a valid positive integer.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\d+$").WithMessage("PhoneNumber must contain digits only.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(50).When(x => x.DisplayName != null);

        RuleFor(x => x.AddressCountry)
            .MaximumLength(100).When(x => x.AddressCountry != null);

        RuleFor(x => x.AddressCity)
            .MaximumLength(100).When(x => x.AddressCity != null);

        RuleFor(x => x.AddressPostalCode)
            .MaximumLength(20).When(x => x.AddressPostalCode != null);

        RuleFor(x => x.AddressLine)
            .MaximumLength(200).When(x => x.AddressLine != null);
    }
}
