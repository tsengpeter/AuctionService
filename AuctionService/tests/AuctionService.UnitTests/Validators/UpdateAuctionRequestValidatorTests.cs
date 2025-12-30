using AuctionService.Core.DTOs.Requests;
using AuctionService.Core.Validators;
using FluentValidation.TestHelper;

namespace AuctionService.UnitTests.Validators;

/// <summary>
/// UpdateAuctionRequestValidator 單元測試
/// </summary>
public class UpdateAuctionRequestValidatorTests
{
    private readonly UpdateAuctionRequestValidator _validator;

    public UpdateAuctionRequestValidatorTests()
    {
        _validator = new UpdateAuctionRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = string.Empty,
            Description = "Valid description",
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("商品名稱不能為空");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Too_Short()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "AB",
            Description = "Valid description",
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("商品名稱長度必須在 3-200 個字元之間");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Too_Long()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = new string('A', 201),
            Description = "Valid description",
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("商品名稱長度必須在 3-200 個字元之間");
    }

    [Fact]
    public void Should_Have_Error_When_Description_Is_Empty()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "Valid Name",
            Description = string.Empty,
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("商品描述不能為空");
    }

    [Fact]
    public void Should_Have_Error_When_Description_Is_Too_Long()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "Valid Name",
            Description = new string('A', 2001),
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("商品描述長度不能超過 2000 個字元");
    }

    [Fact]
    public void Should_Have_Error_When_StartingPrice_Is_Zero()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "Valid Name",
            Description = "Valid description",
            StartingPrice = 0,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartingPrice)
            .WithErrorMessage("起標價必須大於 0");
    }

    [Fact]
    public void Should_Have_Error_When_StartingPrice_Is_Negative()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "Valid Name",
            Description = "Valid description",
            StartingPrice = -10,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartingPrice)
            .WithErrorMessage("起標價必須大於 0");
    }

    [Fact]
    public void Should_Have_Error_When_EndTime_Is_Too_Soon()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "Valid Name",
            Description = "Valid description",
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddMinutes(30)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage("拍賣結束時間必須至少在 1 小時之後");
    }

    [Fact]
    public void Should_Have_Error_When_EndTime_Is_In_The_Past()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "Valid Name",
            Description = "Valid description",
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddHours(-1)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage("拍賣結束時間必須至少在 1 小時之後");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        // Arrange
        var request = new UpdateAuctionRequest
        {
            Name = "Valid Name",
            Description = "Valid description",
            StartingPrice = 100,
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}