using AuctionService.Shared.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace AuctionService.UnitTests.Shared;

public record TestRequest(string Name) : IRequest<string>;

public class ValidationBehaviorTests
{

    [Fact]
    public async Task Handle_WhenNoValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns("success");

        var result = await behavior.Handle(new TestRequest("test"), next, CancellationToken.None);

        result.Should().Be("success");
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldCallNext()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns("ok");

        var result = await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Name", "Name is too short")
        };
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        var act = () => behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Name*");
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldContainAllErrors()
    {
        var failures = new List<ValidationFailure>
        {
            new("Field1", "Error 1"),
            new("Field2", "Error 2")
        };
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        var act = () => behavior.Handle(new TestRequest("x"), next, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }
}
