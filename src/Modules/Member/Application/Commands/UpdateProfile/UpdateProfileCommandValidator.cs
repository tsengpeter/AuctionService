using FluentValidation;

namespace Member.Application.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(30);

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
