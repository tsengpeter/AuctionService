using BiddingService.Core.DTOs.Requests;
using BiddingService.Core.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace BiddingService.UnitTests.Validators;

public class CreateBidRequestValidatorTests
{
    private readonly CreateBidRequestValidator _validator;

    public CreateBidRequestValidatorTests()
    {
        _validator = new CreateBidRequestValidator();
    }

    [Fact]
    public void Validate_WhenAuctionIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AuctionId);
    }

    [Fact]
    public void Validate_WhenAuctionIdIsZero_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 0, Amount = 100 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AuctionId)
            .WithErrorMessage("AuctionId must be greater than 0");
    }

    [Fact]
    public void Validate_WhenAuctionIdIsNegative_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = -1, Amount = 100 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AuctionId)
            .WithErrorMessage("AuctionId must be greater than 0");
    }

    [Fact]
    public void Validate_WhenAmountIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100.50m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_WhenAmountIsZero_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than 0");
    }

    [Fact]
    public void Validate_WhenAmountIsNegative_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = -10 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than 0");
    }

    [Fact]
    public void Validate_WhenAmountHasMoreThanTwoDecimalPlaces_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100.123m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must have at most 2 decimal places");
    }

    [Fact]
    public void Validate_WhenAmountHasExactlyTwoDecimalPlaces_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100.12m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_WhenAmountIsWholeNumber_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateBidRequest { AuctionId = 1, Amount = 100 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }
}