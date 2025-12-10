using AuctionService.Core.DTOs.Common;
using AuctionService.Core.Validators;
using FluentValidation.TestHelper;

namespace AuctionService.UnitTests.Validators;

/// <summary>
/// AuctionQueryParametersValidator 單元測試
/// </summary>
public class AuctionQueryParametersValidatorTests
{
    private readonly AuctionQueryParametersValidator _validator;

    public AuctionQueryParametersValidatorTests()
    {
        _validator = new AuctionQueryParametersValidator();
    }

    [Fact]
    public void Should_Have_Error_When_PageNumber_Is_Less_Than_1()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { PageNumber = 0 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PageNumber_Is_Valid()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { PageNumber = 1 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Is_Less_Than_1()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { PageSize = 0 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PageSize_Is_Valid()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { PageSize = 25 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Should_Have_Error_When_SearchTerm_Is_Too_Long()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { SearchTerm = new string('a', 101) };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Fact]
    public void Should_Not_Have_Error_When_SearchTerm_Is_Valid()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { SearchTerm = "valid search term" };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Fact]
    public void Should_Have_Error_When_CategoryId_Is_Less_Than_1()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { CategoryId = 0 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_CategoryId_Is_Valid()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { CategoryId = 1 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Should_Have_Error_When_MinPrice_Is_Negative()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { MinPrice = -1 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldHaveValidationErrorFor(x => x.MinPrice);
    }

    [Fact]
    public void Should_Have_Error_When_MaxPrice_Is_Negative()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { MaxPrice = -1 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }

    [Fact]
    public void Should_Have_Error_When_MinPrice_Is_Greater_Than_MaxPrice()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { MinPrice = 100, MaxPrice = 50 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Price_Range_Is_Valid()
    {
        // Arrange
        var parameters = new AuctionQueryParameters { MinPrice = 50, MaxPrice = 100 };

        // Act & Assert
        var result = _validator.TestValidate(parameters);
        result.ShouldNotHaveValidationErrorFor(x => x.MinPrice);
        result.ShouldNotHaveValidationErrorFor(x => x.MaxPrice);
    }
}