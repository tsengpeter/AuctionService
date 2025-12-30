using AuctionService.Core.DTOs.Responses;
using AuctionService.Core.Entities;
using AuctionService.Core.Interfaces;
using AuctionService.Core.Services;
using FluentAssertions;
using Moq;

namespace AuctionService.UnitTests.Services;

/// <summary>
/// CategoryService 單元測試
/// </summary>
public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _categoryService = new CategoryService(_categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WithActiveCategories_ReturnsOnlyActiveCategoriesOrderedByDisplayOrder()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Category A", DisplayOrder = 2, IsActive = true },
            new Category { Id = 2, Name = "Category B", DisplayOrder = 1, IsActive = true },
            new Category { Id = 3, Name = "Category C", DisplayOrder = 3, IsActive = false }
        };

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _categoryService.GetAllCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Category B"); // DisplayOrder = 1
        result.Last().Name.Should().Be("Category A");  // DisplayOrder = 2
        result.First().Id.Should().Be(2);
        result.Last().Id.Should().Be(1);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WithNoActiveCategories_ReturnsEmptyList()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Category A", DisplayOrder = 1, IsActive = false },
            new Category { Id = 2, Name = "Category B", DisplayOrder = 2, IsActive = false }
        };

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _categoryService.GetAllCategoriesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WithEmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _categoryService.GetAllCategoriesAsync();

        // Assert
        result.Should().BeEmpty();
    }
}