using AuctionService.Shared;
using FluentAssertions;

namespace AuctionService.UnitTests.Shared;

public class ApiResponseTests
{
    [Fact]
    public void Ok_ShouldReturnSuccessResponse_WithData()
    {
        var result = ApiResponseFactory.Ok("hello");

        result.Success.Should().BeTrue();
        result.Data.Should().Be("hello");
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Ok_ShouldReturnCustomStatusCode()
    {
        var result = ApiResponseFactory.Ok("created", 201);

        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public void Ok_ShouldAllowNullData()
    {
        var result = ApiResponseFactory.Ok<string?>(null);

        result.Success.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    [Fact]
    public void Fail_ShouldReturnErrorResponse()
    {
        var result = ApiResponseFactory.Fail("Not found", 404);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Not found");
        result.StatusCode.Should().Be(404);
        result.Errors.Should().BeNull();
    }

    [Fact]
    public void ValidationFail_ShouldReturn422WithErrors()
    {
        var errors = new[]
        {
            new FieldError("Email", "Email is required"),
            new FieldError("Username", "Username must be at least 3 characters")
        };

        var result = ApiResponseFactory.ValidationFail(errors);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Errors.Should().HaveCount(2);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ValidationFail_ShouldContainCorrectFieldErrors()
    {
        var errors = new[] { new FieldError("Email", "Invalid email format") };

        var result = ApiResponseFactory.ValidationFail(errors);

        result.Errors!.First().Field.Should().Be("Email");
        result.Errors!.First().Message.Should().Be("Invalid email format");
    }
}
