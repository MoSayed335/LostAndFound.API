using FluentAssertions;
using LostAndFound.Application.Features.Matching.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using LostAndFound.Infrastructure.Services;
using LostAndFound.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LostAndFound.Tests.Jobs;

public class BackgroundJobServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IItemRepository> _itemRepoMock;
    private readonly Mock<IClaimRepository> _claimRepoMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<IMatchingService> _matchingServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<BackgroundJobService>> _loggerMock;
    private readonly BackgroundJobService _sut;

    public BackgroundJobServiceTests()
    {
        (_uowMock, _itemRepoMock, _claimRepoMock, _categoryRepoMock) = MockServiceFactory.CreateMockUnitOfWork();
        _matchingServiceMock = new Mock<IMatchingService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<BackgroundJobService>>();

        _sut = new BackgroundJobService(
            _uowMock.Object,
            _matchingServiceMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessItemMatchesAsync_WhenItemExistsAndActive_ShouldEvaluateMatches()
    {
        // Arrange: 1. Fire-and-Forget Job
        var item = ItemFactory.CreateItem(id: 1, type: ItemType.Lost, status: ItemStatus.Active);
        var candidateFoundItem = ItemFactory.CreateItem(id: 2, type: ItemType.Found, status: ItemStatus.Active);

        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _itemRepoMock.Setup(r => r.GetActiveItemsByTypeAsync(ItemType.Found, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Item> { candidateFoundItem });

        var matchResult = new MatchResultDto
        {
            ItemId = candidateFoundItem.Id,
            Title = candidateFoundItem.Title,
            MatchScore = 85,
            MatchingReasons = new List<string> { "Same category", "Same location" }
        };

        _matchingServiceMock.Setup(m => m.FindMatches(item, It.IsAny<IEnumerable<Item>>()))
            .Returns(new List<MatchResultDto> { matchResult });

        // Act
        await _sut.ProcessItemMatchesAsync(1, CancellationToken.None);

        // Assert
        _matchingServiceMock.Verify(m => m.FindMatches(item, It.IsAny<IEnumerable<Item>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessItemMatchesAsync_WhenItemNotFound_ShouldAbortGracefully()
    {
        // Arrange
        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(999, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Item?)null);

        // Act
        await _sut.ProcessItemMatchesAsync(999, CancellationToken.None);

        // Assert
        _matchingServiceMock.Verify(m => m.FindMatches(It.IsAny<Item>(), It.IsAny<IEnumerable<Item>>()), Times.Never);
    }

    [Fact]
    public async Task CheckPendingClaimReminderAsync_WhenClaimIsPending_ShouldDispatchReminder()
    {
        // Arrange: 2. Delayed Job
        var claim = ClaimFactory.CreateClaim(id: 10, status: ClaimStatus.Pending);

        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        // Act
        await _sut.CheckPendingClaimReminderAsync(10, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(n => n.SendClaimReminderNotificationAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckPendingClaimReminderAsync_WhenClaimAlreadyApproved_ShouldSkipReminder()
    {
        // Arrange: 2. Delayed Job (Graceful bypass if already reviewed)
        var claim = ClaimFactory.CreateClaim(id: 10, status: ClaimStatus.Approved);

        _claimRepoMock.Setup(r => r.GetByIdWithItemAndClaimantAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        // Act
        await _sut.CheckPendingClaimReminderAsync(10, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(n => n.SendClaimReminderNotificationAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanupStaleItemsAsync_WhenStaleItemsExist_ShouldArchiveToClosedAndSave()
    {
        // Arrange: 3. Recurring Job
        var oldClaimedItem = ItemFactory.CreateItem(id: 101, status: ItemStatus.Claimed);
        oldClaimedItem.CreatedAt = DateTime.UtcNow.AddDays(-45);
        oldClaimedItem.UpdatedAt = DateTime.UtcNow.AddDays(-35);

        var itemsList = new List<Item> { oldClaimedItem }.AsQueryable();
        _itemRepoMock.Setup(r => r.GetQueryable(false)).Returns(itemsList);

        // Act
        await _sut.CleanupStaleItemsAsync(CancellationToken.None);

        // Assert
        oldClaimedItem.Status.Should().Be(ItemStatus.Closed);
        _itemRepoMock.Verify(r => r.Update(oldClaimedItem), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendItemProcessingSummaryAsync_WhenItemExists_ShouldSendMatchesNotification()
    {
        // Arrange: 4. Continuation Job
        var item = ItemFactory.CreateItem(id: 5, type: ItemType.Lost, status: ItemStatus.Active);
        var candidateFoundItem = ItemFactory.CreateItem(id: 6, type: ItemType.Found, status: ItemStatus.Active);

        _itemRepoMock.Setup(r => r.GetByIdWithDetailsAsync(5, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepoMock.Setup(r => r.GetActiveItemsByTypeAsync(ItemType.Found, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Item> { candidateFoundItem });

        _matchingServiceMock.Setup(m => m.FindMatches(item, It.IsAny<IEnumerable<Item>>()))
            .Returns(new List<MatchResultDto>
            {
                new() { ItemId = 6, Title = "Found item", MatchScore = 90 }
            });

        // Act
        await _sut.SendItemProcessingSummaryAsync(5, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(n => n.SendItemMatchesNotificationAsync(5, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
