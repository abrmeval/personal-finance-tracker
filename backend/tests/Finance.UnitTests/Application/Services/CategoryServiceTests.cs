using Microsoft.Extensions.Logging;
using NSubstitute;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Infrastructure.Services;
using Personal.FinanceTracker.Shared.Constants;

namespace Finance.UnitTests.Application.Services;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly ILogger<CategoryService> _logger = Substitute.For<ILogger<CategoryService>>();
    private readonly CategoryService _categoryService;
    private static readonly Guid UserId = Guid.NewGuid();

    public CategoryServiceTests()
    {
        _categoryService = new CategoryService(_repository, _logger);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedCategories()
    {
        var categories = new List<Category>
        {
            Category.Create(UserId, "Food", "🍔", "#FF0000"),
            Category.Create(UserId, "Transport", "🚗", "#00FF00")
        };
        _repository.GetAllByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns(categories);

        var result = await _categoryService.GetAllAsync(UserId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Food", result.Value[0].Name);
        Assert.Equal("Transport", result.Value[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyResult()
    {
        _repository.GetAllByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _categoryService.GetAllAsync(UserId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCategory_ReturnsCategory()
    {
        var category = Category.Create(UserId, "Food", "🍔", "#FF0000");
        _repository.GetByUserAndIdAsync(UserId, category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _categoryService.GetByIdAsync(UserId, category.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(category.Id, result.Value!.Id);
        Assert.Equal("Food", result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repository.GetByUserAndIdAsync(UserId, id, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _categoryService.GetByIdAsync(UserId, id);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.CategoryNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CreateAsync_NewCategory_CreatesAndReturnsCategory()
    {
        var request = new CreateCategoryRequest("Food", "🍔", "#FF0000");
        _repository.ExistsByUserAndNameAsync(UserId, request.Name, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _categoryService.CreateAsync(UserId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Food", result.Value!.Name);
        await _repository.Received(1).AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsFailure()
    {
        var request = new CreateCategoryRequest("Food", null, null);
        _repository.ExistsByUserAndNameAsync(UserId, request.Name, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _categoryService.CreateAsync(UserId, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.DuplicateCategoryName, result.Error!.Code);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ExistingCategory_UpdatesAndReturnsCategory()
    {
        var category = Category.Create(UserId, "Old Name", "old-icon", "#000000");
        var request = new UpdateCategoryRequest("New Name", "new-icon", "#FFFFFF");
        _repository.GetByUserAndIdAsync(UserId, category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _repository.ExistsByUserAndNameAsync(UserId, request.Name, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _categoryService.UpdateAsync(UserId, category.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal("new-icon", result.Value.Icon);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Name", null, null);
        _repository.GetByUserAndIdAsync(UserId, id, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _categoryService.UpdateAsync(UserId, id, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.CategoryNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameDifferentCategory_ReturnsFailure()
    {
        var category = Category.Create(UserId, "Old Name", null, null);
        var request = new UpdateCategoryRequest("Existing Name", null, null);
        _repository.GetByUserAndIdAsync(UserId, category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _repository.ExistsByUserAndNameAsync(UserId, request.Name, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _categoryService.UpdateAsync(UserId, category.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.DuplicateCategoryName, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateAsync_SameNameAllowed_UpdatesSuccessfully()
    {
        var category = Category.Create(UserId, "Same Name", null, null);
        var request = new UpdateCategoryRequest("Same Name", "new-icon", null);
        _repository.GetByUserAndIdAsync(UserId, category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _repository.ExistsByUserAndNameAsync(UserId, request.Name, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _categoryService.UpdateAsync(UserId, category.Id, request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCategory_DeletesAndReturnsTrue()
    {
        var category = Category.Create(UserId, "Food", null, null);
        _repository.GetByUserAndIdAsync(UserId, category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _categoryService.DeleteAsync(UserId, category.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await _repository.Received(1).DeleteAsync(category, Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repository.GetByUserAndIdAsync(UserId, id, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _categoryService.DeleteAsync(UserId, id);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.CategoryNotFound, result.Error!.Code);
    }
}
