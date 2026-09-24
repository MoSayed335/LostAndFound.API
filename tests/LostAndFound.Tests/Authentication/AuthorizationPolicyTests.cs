using FluentAssertions;
using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.Commands.ApproveClaim;
using LostAndFound.Application.Features.Claims.Commands.RejectClaim;
using LostAndFound.Application.Features.Items.Commands.DeleteItem;
using LostAndFound.Application.Features.Items.Commands.ReturnItem;
using LostAndFound.Application.Features.Items.Commands.UpdateItem;
using LostAndFound.Application.Features.Matching.Queries.GetItemMatches;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using LostAndFound.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LostAndFound.Tests.Authentication;

public class AuthorizationPolicyTests
{
    private const int UserAId = 10;
    private const int UserBId = 20;
    private const int AdminId = 999;

    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IItemRepository> _itemRepoMock;
    private readonly Mock<IClaimRepository> _claimRepoMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<IFileService> _fileServiceMock = new();
    private readonly Mock<IMatchingService> _matchingServiceMock = new();

    public AuthorizationPolicyTests()
    {
        (_uowMock, _itemRepoMock, _claimRepoMock, _categoryRepoMock) = MockServiceFactory.CreateMockUnitOfWork();
    }

    [Fact]
    public async Task UpdateItem_WhenUserBAttemptsToUpdateItemA_ShouldBeForbidden()
    {
        // Arrange
        var itemA = ItemFactory.CreateItem(id: 1, userId: UserAId);
        _itemRepoMock.Setup(r => r.GetByIdAsync(itemA.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(itemA);

        var handler = new UpdateItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, Mock.Of<ILogger<UpdateItemCommandHandler>>());

        var command = new UpdateItemCommand(
            itemA.Id, "Hacked Title", "Hacked Desc", 1, "Hacked Location", DateTime.UtcNow.AddDays(-1), null, ItemStatus.Active, CurrentUserId: UserBId, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteItem_WhenUserBAttemptsToDeleteItemA_ShouldBeForbidden()
    {
        // Arrange
        var itemA = ItemFactory.CreateItem(id: 1, userId: UserAId);
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(itemA.Id, It.IsAny<CancellationToken>())).ReturnsAsync(itemA);

        var handler = new DeleteItemCommandHandler(
            _uowMock.Object, _fileServiceMock.Object, Mock.Of<ILogger<DeleteItemCommandHandler>>());

        var command = new DeleteItemCommand(itemA.Id, CurrentUserId: UserBId, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task ApproveClaim_WhenUserBAttemptsToApproveClaimOnItemA_ShouldBeForbidden()
    {
        // Arrange
        var itemA = ItemFactory.CreateItem(id: 1, userId: UserAId);
        var claim = ClaimFactory.CreateClaim(id: 100, itemId: itemA.Id, claimantId: 30, status: ClaimStatus.Pending, item: itemA);
        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(claim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(claim);

        var handler = new ApproveClaimCommandHandler(
            _uowMock.Object, Mock.Of<ILogger<ApproveClaimCommandHandler>>());

        var command = new ApproveClaimCommand(claim.Id, CurrentUserId: UserBId, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task RejectClaim_WhenUserBAttemptsToRejectClaimOnItemA_ShouldBeForbidden()
    {
        // Arrange
        var itemA = ItemFactory.CreateItem(id: 1, userId: UserAId);
        var claim = ClaimFactory.CreateClaim(id: 100, itemId: itemA.Id, claimantId: 30, status: ClaimStatus.Pending, item: itemA);
        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(claim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(claim);

        var handler = new RejectClaimCommandHandler(
            _uowMock.Object, Mock.Of<ILogger<RejectClaimCommandHandler>>());

        var command = new RejectClaimCommand(claim.Id, CurrentUserId: UserBId, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task ReturnItem_WhenUserBAttemptsToReturnItemA_ShouldBeForbidden()
    {
        // Arrange
        var itemA = ItemFactory.CreateItem(id: 1, userId: UserAId, status: ItemStatus.Claimed);
        _itemRepoMock.Setup(r => r.GetByIdAsync(itemA.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(itemA);

        var handler = new ReturnItemCommandHandler(
            _uowMock.Object, Mock.Of<ILogger<ReturnItemCommandHandler>>());

        var command = new ReturnItemCommand(itemA.Id, CurrentUserId: UserBId, IsAdmin: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task GetMatches_WhenUserBAttemptsToViewMatchesForUserAItem_ShouldBeForbidden()
    {
        // Arrange
        var itemA = ItemFactory.CreateItem(id: 1, userId: UserAId);
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(itemA.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(itemA);

        var handler = new GetItemMatchesQueryHandler(
            _uowMock.Object, _matchingServiceMock.Object, Mock.Of<ILogger<GetItemMatchesQueryHandler>>());

        var query = new GetItemMatchesQuery(itemA.Id, CurrentUserId: UserBId, IsAdmin: false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Admin_CanPerformActionsOnOtherUsersItems()
    {
        // Arrange
        var itemA = ItemFactory.CreateItem(id: 1, userId: UserAId, status: ItemStatus.Claimed);
        _itemRepoMock.Setup(r => r.GetByIdAsync(itemA.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(itemA);
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(itemA.Id, It.IsAny<CancellationToken>())).ReturnsAsync(itemA);
        _claimRepoMock.Setup(r => r.GetPendingClaimsForItemAsync(itemA.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Claim>());

        var returnHandler = new ReturnItemCommandHandler(
            _uowMock.Object, Mock.Of<ILogger<ReturnItemCommandHandler>>());

        var returnCommand = new ReturnItemCommand(itemA.Id, CurrentUserId: AdminId, IsAdmin: true);

        // Act
        var returnResult = await returnHandler.Handle(returnCommand, CancellationToken.None);

        // Assert
        returnResult.Succeeded.Should().BeTrue();
        itemA.Status.Should().Be(ItemStatus.Returned);
    }
}
