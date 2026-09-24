using FluentAssertions;
using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.Commands.ApproveClaim;
using LostAndFound.Application.Features.Claims.Commands.CreateClaim;
using LostAndFound.Application.Features.Claims.Commands.RejectClaim;
using LostAndFound.Application.Features.Items.Commands.ReturnItem;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using LostAndFound.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LostAndFound.Tests.Claims;

public class ClaimBusinessRuleTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IItemRepository> _itemRepoMock;
    private readonly Mock<IClaimRepository> _claimRepoMock;

    private readonly Mock<ILogger<CreateClaimCommandHandler>> _createLoggerMock = new();
    private readonly Mock<ILogger<ApproveClaimCommandHandler>> _approveLoggerMock = new();
    private readonly Mock<ILogger<RejectClaimCommandHandler>> _rejectLoggerMock = new();
    private readonly Mock<ILogger<ReturnItemCommandHandler>> _returnLoggerMock = new();

    public ClaimBusinessRuleTests()
    {
        (_uowMock, _itemRepoMock, _claimRepoMock, _) = MockServiceFactory.CreateMockUnitOfWork();
    }

    [Fact]
    public async Task CreateClaim_WhenUserClaimsOwnItem_ShouldReturnConflict()
    {
        // Arrange
        var itemOwnerId = 10;
        var item = ItemFactory.CreateItem(id: 1, userId: itemOwnerId, status: ItemStatus.Active);
        _itemRepoMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new CreateClaimCommandHandler(_uowMock.Object, _createLoggerMock.Object);
        var command = new CreateClaimCommand(item.Id, "This is mine", itemOwnerId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("You cannot claim your own item");
    }

    [Fact]
    public async Task CreateClaim_WhenItemIsNotActive_ShouldReturnConflict()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Claimed);
        _itemRepoMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new CreateClaimCommandHandler(_uowMock.Object, _createLoggerMock.Object);
        var command = new CreateClaimCommand(item.Id, "I lost this", 20);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("active items");
    }

    [Fact]
    public async Task CreateClaim_WhenDuplicatePendingClaimExists_ShouldReturnConflict()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        _itemRepoMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _claimRepoMock.Setup(r => r.HasPendingClaimAsync(item.Id, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateClaimCommandHandler(_uowMock.Object, _createLoggerMock.Object);
        var command = new CreateClaimCommand(item.Id, "Trying again", 20);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("already have a pending claim");
    }

    [Fact]
    public async Task CreateClaim_WhenValid_ShouldCreatePendingClaim()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        _itemRepoMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _claimRepoMock.Setup(r => r.HasPendingClaimAsync(item.Id, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Claim? addedClaim = null;
        _claimRepoMock.Setup(r => r.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()))
            .Callback<Claim, CancellationToken>((c, _) => addedClaim = c)
            .Returns(Task.CompletedTask);

        var handler = new CreateClaimCommandHandler(_uowMock.Object, _createLoggerMock.Object);
        var command = new CreateClaimCommand(item.Id, "Matches my lost watch", 20);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(ClaimStatus.Pending.ToString());
        addedClaim.Should().NotBeNull();
        addedClaim!.Status.Should().Be(ClaimStatus.Pending);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveClaim_WhenCallerIsNotOwnerNorAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        var claim = ClaimFactory.CreateClaim(id: 100, itemId: item.Id, claimantId: 20, status: ClaimStatus.Pending, item: item);

        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(claim.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        var handler = new ApproveClaimCommandHandler(_uowMock.Object, _approveLoggerMock.Object);
        var command = new ApproveClaimCommand(claim.Id, CurrentUserId: 99, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task ApproveClaim_WhenClaimIsNotPending_ShouldReturnConflict()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        var claim = ClaimFactory.CreateClaim(id: 100, itemId: item.Id, claimantId: 20, status: ClaimStatus.Approved, item: item);

        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(claim.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        var handler = new ApproveClaimCommandHandler(_uowMock.Object, _approveLoggerMock.Object);
        var command = new ApproveClaimCommand(claim.Id, CurrentUserId: 10, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("Only pending claims can be approved");
    }

    [Fact]
    public async Task ApproveClaim_WhenValid_ShouldMarkClaimApproved_ItemClaimed_AndRejectOtherPendingClaims()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        var claim = ClaimFactory.CreateClaim(id: 100, itemId: item.Id, claimantId: 20, status: ClaimStatus.Pending, item: item);
        var otherClaim = ClaimFactory.CreateClaim(id: 101, itemId: item.Id, claimantId: 30, status: ClaimStatus.Pending, item: item);

        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(claim.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);
        _claimRepoMock.Setup(r => r.GetPendingClaimsForItemAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim> { claim, otherClaim });

        var handler = new ApproveClaimCommandHandler(_uowMock.Object, _approveLoggerMock.Object);
        var command = new ApproveClaimCommand(claim.Id, CurrentUserId: 10, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        claim.Status.Should().Be(ClaimStatus.Approved);
        claim.ReviewedAt.Should().NotBeNull();
        item.Status.Should().Be(ItemStatus.Claimed);
        otherClaim.Status.Should().Be(ClaimStatus.Rejected);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectClaim_WhenValid_ShouldMarkClaimRejected_AndKeepItemActive()
    {
        // Arrange
        var item = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        var claim = ClaimFactory.CreateClaim(id: 100, itemId: item.Id, claimantId: 20, status: ClaimStatus.Pending, item: item);

        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(claim.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        var handler = new RejectClaimCommandHandler(_uowMock.Object, _rejectLoggerMock.Object);
        var command = new RejectClaimCommand(claim.Id, CurrentUserId: 10, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        claim.Status.Should().Be(ClaimStatus.Rejected);
        claim.ReviewedAt.Should().NotBeNull();
        item.Status.Should().Be(ItemStatus.Active);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnItem_WhenItemIsNotClaimed_ShouldReturnConflict()
    {
        // Arrange
        var activeItem = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Active);
        _itemRepoMock.Setup(r => r.GetByIdAsync(activeItem.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeItem);

        var handler = new ReturnItemCommandHandler(_uowMock.Object, _returnLoggerMock.Object);
        var command = new ReturnItemCommand(activeItem.Id, CurrentUserId: 10, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("Only claimed items can be marked as returned");
    }

    [Fact]
    public async Task ReturnItem_WhenClaimedItemReturnedByOwner_ShouldTransitionToReturned()
    {
        // Arrange
        var claimedItem = ItemFactory.CreateItem(id: 1, userId: 10, status: ItemStatus.Claimed);
        _itemRepoMock.Setup(r => r.GetByIdAsync(claimedItem.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(claimedItem);
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(claimedItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claimedItem);
        _claimRepoMock.Setup(r => r.GetPendingClaimsForItemAsync(claimedItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        var handler = new ReturnItemCommandHandler(_uowMock.Object, _returnLoggerMock.Object);
        var command = new ReturnItemCommand(claimedItem.Id, CurrentUserId: 10, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        claimedItem.Status.Should().Be(ItemStatus.Returned);
        claimedItem.UpdatedAt.Should().NotBeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
