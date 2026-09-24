using FluentAssertions;
using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.Commands.CreateFoundItem;
using LostAndFound.Application.Features.Items.Commands.CreateLostItem;
using LostAndFound.Application.Features.Items.Commands.DeleteItem;
using LostAndFound.Application.Features.Items.Commands.UpdateItem;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using LostAndFound.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LostAndFound.Tests.Items;

public class ItemFeatureAndValidationTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IItemRepository> _itemRepoMock;
    private readonly Mock<IClaimRepository> _claimRepoMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<IFileService> _fileServiceMock = new();

    private readonly Mock<ILogger<CreateLostItemCommandHandler>> _lostLoggerMock = new();
    private readonly Mock<ILogger<CreateFoundItemCommandHandler>> _foundLoggerMock = new();
    private readonly Mock<ILogger<UpdateItemCommandHandler>> _updateLoggerMock = new();
    private readonly Mock<ILogger<DeleteItemCommandHandler>> _deleteLoggerMock = new();

    public ItemFeatureAndValidationTests()
    {
        (_uowMock, _itemRepoMock, _claimRepoMock, _categoryRepoMock) = MockServiceFactory.CreateMockUnitOfWork();
    }

    [Fact]
    public async Task CreateLostItem_WhenValid_ShouldSetStatusActive_AndServerControlledFields()
    {
        // Arrange
        var category = CategoryFactory.CreateCategory(1, "Electronics");
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        Item? createdItem = null;
        _itemRepoMock.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
            .Callback<Item, CancellationToken>((item, _) =>
            {
                item.Id = 42;
                item.Category = category;
                createdItem = item;
            })
            .Returns(Task.CompletedTask);

        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => createdItem);

        var handler = new CreateLostItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, _lostLoggerMock.Object);

        var command = new CreateLostItemCommand(
            "AirPods Pro", "White case with sticker", 1, "Gym locker", DateTime.UtcNow.AddDays(-1), null, 10);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(ItemType.Lost.ToString());
        result.Value.Status.Should().Be(ItemStatus.Active.ToString());
        result.Value.Owner?.Id.Should().Be(10);

        createdItem.Should().NotBeNull();
        createdItem!.Status.Should().Be(ItemStatus.Active);
        createdItem.UserId.Should().Be(10);
        createdItem.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateFoundItem_WhenValid_ShouldSetTypeFound_AndStatusActive()
    {
        // Arrange
        var category = CategoryFactory.CreateCategory(2, "Bags");
        _categoryRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        Item? createdItem = null;
        _itemRepoMock.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
            .Callback<Item, CancellationToken>((item, _) =>
            {
                item.Id = 43;
                item.Category = category;
                createdItem = item;
            })
            .Returns(Task.CompletedTask);

        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => createdItem);

        var handler = new CreateFoundItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, _foundLoggerMock.Object);

        var command = new CreateFoundItemCommand(
            "Backpack", "Black Herschel backpack", 2, "Cafeteria", DateTime.UtcNow.AddDays(-1), null, 15);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value!.Type.Should().Be(ItemType.Found.ToString());
        result.Value.Status.Should().Be(ItemStatus.Active.ToString());
    }

    [Fact]
    public async Task UpdateItem_WhenUserIsNotOwnerNorAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10);
        _itemRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new UpdateItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, _updateLoggerMock.Object);

        var command = new UpdateItemCommand(
            1, "Updated Title", "Updated Desc", 1, "New Location", DateTime.UtcNow.AddDays(-1), null, ItemStatus.Active, CurrentUserId: 99, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateItem_WhenStatusDirectlySetToClaimedOrReturned_ShouldReturnValidationError()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        _itemRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new UpdateItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, _updateLoggerMock.Object);

        var commandClaimed = new UpdateItemCommand(
            1, "Title", "Desc", 1, "Location", DateTime.UtcNow.AddDays(-1), null, ItemStatus.Claimed, CurrentUserId: 10, IsAdmin: false);

        var commandReturned = new UpdateItemCommand(
            1, "Title", "Desc", 1, "Location", DateTime.UtcNow.AddDays(-1), null, ItemStatus.Returned, CurrentUserId: 10, IsAdmin: false);

        // Act & Assert
        var resultClaimed = await handler.Handle(commandClaimed, CancellationToken.None);
        resultClaimed.Succeeded.Should().BeFalse();
        resultClaimed.ErrorType.Should().Be(ResultErrorType.Validation);
        resultClaimed.Error.Should().Contain("cannot be set directly to Claimed or Returned");

        var resultReturned = await handler.Handle(commandReturned, CancellationToken.None);
        resultReturned.Succeeded.Should().BeFalse();
        resultReturned.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public async Task UpdateItem_WhenAdminUpdatesOtherUsersItem_ShouldSucceed()
    {
        // Arrange
        var category = CategoryFactory.CreateCategory(1);
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        _itemRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new UpdateItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, _updateLoggerMock.Object);

        var command = new UpdateItemCommand(
            1, "Admin Updated Title", "Admin Updated Description", 1, "Updated Location", DateTime.UtcNow.AddDays(-1), null, ItemStatus.Active, CurrentUserId: 999, IsAdmin: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        item.Title.Should().Be("Admin Updated Title");
        item.UpdatedAt.Should().NotBeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteItem_WhenCallerIsNotOwnerNorAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10);
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new DeleteItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, _deleteLoggerMock.Object);

        var command = new DeleteItemCommand(1, CurrentUserId: 99, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteItem_WhenItemHasImage_ShouldDeletePhysicalFile()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, imageUrl: "/uploads/items/test-image.jpg");
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new DeleteItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, _deleteLoggerMock.Object);

        var command = new DeleteItemCommand(1, CurrentUserId: 10, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _fileServiceMock.Verify(f => f.DeleteFileAsync("/uploads/items/test-image.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _itemRepoMock.Verify(r => r.Delete(item), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Validation Tests
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task CreateLostItemCommandValidator_WhenTitleIsTooShort_ShouldHaveValidationError(string title)
    {
        _categoryRepoMock.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var validator = new CreateLostItemCommandValidator(_categoryRepoMock.Object);
        var command = new CreateLostItemCommand(
            title, "Valid description here", 1, "Valid Location", DateTime.UtcNow.AddDays(-1), null, 1);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLostItemCommand.Title));
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    public async Task CreateLostItemCommandValidator_WhenLocationIsTooShort_ShouldHaveValidationError(string location)
    {
        _categoryRepoMock.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var validator = new CreateLostItemCommandValidator(_categoryRepoMock.Object);
        var command = new CreateLostItemCommand(
            "Valid Title", "Valid description here", 1, location, DateTime.UtcNow.AddDays(-1), null, 1);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLostItemCommand.Location));
    }

    [Fact]
    public async Task CreateLostItemCommandValidator_WhenDateIsInFuture_ShouldHaveValidationError()
    {
        _categoryRepoMock.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var validator = new CreateLostItemCommandValidator(_categoryRepoMock.Object);
        var command = new CreateLostItemCommand(
            "Valid Title", "Valid description here", 1, "Valid Location", DateTime.UtcNow.AddDays(2), null, 1);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLostItemCommand.DateLostOrFound));
    }

    [Fact]
    public async Task CreateLostItemCommandValidator_WhenDateIsOlderThanTenYears_ShouldHaveValidationError()
    {
        _categoryRepoMock.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var validator = new CreateLostItemCommandValidator(_categoryRepoMock.Object);
        var command = new CreateLostItemCommand(
            "Valid Title", "Valid description here", 1, "Valid Location", DateTime.UtcNow.AddYears(-11), null, 1);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLostItemCommand.DateLostOrFound));
    }
}
