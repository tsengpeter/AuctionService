using FluentValidation;

namespace Member.Application.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RawToken).NotEmpty();
    }
}
