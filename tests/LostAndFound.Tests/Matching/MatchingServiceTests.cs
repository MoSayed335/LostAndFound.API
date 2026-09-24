using FluentAssertions;
using LostAndFound.Application.Features.Matching.Options;
using LostAndFound.Application.Services;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using LostAndFound.Tests.Common;
using Xunit;

namespace LostAndFound.Tests.Matching;

public class MatchingServiceTests
{
    private readonly MatchingService _sut;

    public MatchingServiceTests()
    {
        _sut = new MatchingService(new MatchingOptions
        {
            CategoryWeight = 25,
            LocationWeight = 20,
            DateWeight = 20,
            TitleWeight = 20,
            DescriptionWeight = 15,
            MinimumMatchScore = 30
        });
    }

    [Fact]
    public void CalculateMatch_WhenItemsHaveSameType_ShouldReturnNull()
    {
        // Arrange
        var source = ItemFactory.CreateItem(id: 1, type: ItemType.Lost);
        var candidate = ItemFactory.CreateItem(id: 2, type: ItemType.Lost);

        // Act
        var result = _sut.CalculateMatch(source, candidate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateMatch_WhenCandidateIsNotActive_ShouldReturnNull()
    {
        // Arrange
        var source = ItemFactory.CreateItem(id: 1, type: ItemType.Lost, status: ItemStatus.Active);
        var candidateClaimed = ItemFactory.CreateItem(id: 2, type: ItemType.Found, status: ItemStatus.Claimed);
        var candidateReturned = ItemFactory.CreateItem(id: 3, type: ItemType.Found, status: ItemStatus.Returned);

        // Act & Assert
        _sut.CalculateMatch(source, candidateClaimed).Should().BeNull();
        _sut.CalculateMatch(source, candidateReturned).Should().BeNull();
    }

    [Fact]
    public void CalculateMatch_WhenCandidateIsSameItem_ShouldReturnNull()
    {
        // Arrange
        var source = ItemFactory.CreateItem(id: 5, type: ItemType.Lost);
        var candidate = ItemFactory.CreateItem(id: 5, type: ItemType.Found);

        // Act
        var result = _sut.CalculateMatch(source, candidate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateMatch_WhenItemsHaveExactMatchAcrossAllAttributes_ShouldYieldHighMatchScore()
    {
        // Arrange
        var date = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        var source = ItemFactory.CreateItem(
            id: 1,
            title: "iPhone 15 Pro Blue",
            description: "Blue titanium with cracked screen protector",
            type: ItemType.Lost,
            categoryId: 1,
            location: "Main Campus Library 2nd Floor",
            dateLostOrFound: date);

        var candidate = ItemFactory.CreateItem(
            id: 2,
            title: "iPhone 15 Pro Blue",
            description: "Blue titanium with cracked screen protector",
            type: ItemType.Found,
            categoryId: 1,
            location: "Main Campus Library 2nd Floor",
            dateLostOrFound: date);

        // Act
        var result = _sut.CalculateMatch(source, candidate);

        // Assert
        result.Should().NotBeNull();
        result!.MatchScore.Should().Be(100);
        result.MatchingReasons.Should().Contain(r => r.Contains("category", StringComparison.OrdinalIgnoreCase));
        result.MatchingReasons.Should().Contain(r => r.Contains("location", StringComparison.OrdinalIgnoreCase));
        result.MatchingReasons.Should().Contain(r => r.Contains("date", StringComparison.OrdinalIgnoreCase));
        result.MatchingReasons.Should().Contain(r => r.Contains("title", StringComparison.OrdinalIgnoreCase));
        result.MatchingReasons.Should().Contain(r => r.Contains("description", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CalculateMatch_WhenCategoriesDiffer_ShouldNotAwardCategoryPoints()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var source = ItemFactory.CreateItem(id: 1, categoryId: 1, type: ItemType.Lost, dateLostOrFound: date);
        var candidate = ItemFactory.CreateItem(id: 2, categoryId: 2, type: ItemType.Found, dateLostOrFound: date);

        // Act
        var result = _sut.CalculateMatch(source, candidate);

        // Assert
        result.Should().NotBeNull();
        result!.MatchingReasons.Should().NotContain("Same category");
    }

    [Fact]
    public void CalculateMatch_WhenLocationsMatchPartially_ShouldAwardPartialLocationPoints()
    {
        // Arrange
        var source = ItemFactory.CreateItem(
            id: 1,
            type: ItemType.Lost,
            location: "Central Library Room 302",
            categoryId: 1);

        var candidate = ItemFactory.CreateItem(
            id: 2,
            type: ItemType.Found,
            location: "Central Library",
            categoryId: 1);

        // Act
        var result = _sut.CalculateMatch(source, candidate);

        // Assert
        result.Should().NotBeNull();
        result!.MatchingReasons.Should().Contain(r => r.Contains("location", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CalculateMatch_WhenDateDifferenceIsLarge_ShouldNotAwardDatePoints()
    {
        // Arrange
        var source = ItemFactory.CreateItem(id: 1, type: ItemType.Lost, dateLostOrFound: DateTime.UtcNow.AddDays(-60));
        var candidate = ItemFactory.CreateItem(id: 2, type: ItemType.Found, dateLostOrFound: DateTime.UtcNow);

        // Act
        var result = _sut.CalculateMatch(source, candidate);

        // Assert
        result.Should().NotBeNull();
        result!.MatchingReasons.Should().NotContain(r => r.Contains("date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CalculateMatch_WhenItemsAreCompletelyUnrelated_ShouldScoreZero()
    {
        // Arrange
        var source = ItemFactory.CreateItem(
            id: 1,
            title: "Black Leather Wallet",
            description: "Has credit cards inside",
            type: ItemType.Lost,
            categoryId: 1,
            location: "North Cafeteria",
            dateLostOrFound: DateTime.UtcNow.AddDays(-90));

        var candidate = ItemFactory.CreateItem(
            id: 2,
            title: "Silver MacBook Pro M3",
            description: "Silver laptop in dark gray sleeve",
            type: ItemType.Found,
            categoryId: 2,
            location: "South Sports Gym",
            dateLostOrFound: DateTime.UtcNow);

        // Act
        var result = _sut.CalculateMatch(source, candidate);

        // Assert
        result.Should().NotBeNull();
        result!.MatchScore.Should().Be(0);
        result.MatchingReasons.Should().BeEmpty();
    }

    [Fact]
    public void FindMatches_WhenCandidatesScoreBelowThreshold_ShouldFilterThemOut()
    {
        // Arrange
        var source = ItemFactory.CreateItem(id: 1, type: ItemType.Lost, title: "Keys", location: "Park", categoryId: 1);
        var unrelatedCandidate = ItemFactory.CreateItem(
            id: 2,
            type: ItemType.Found,
            title: "Scarf",
            description: "Red wool scarf",
            location: "Cinema",
            categoryId: 2,
            dateLostOrFound: DateTime.UtcNow.AddDays(-100));

        // Act
        var matches = _sut.FindMatches(source, new[] { unrelatedCandidate });

        // Assert
        matches.Should().BeEmpty();
    }

    [Fact]
    public void FindMatches_ShouldReturnResultsOrderedByScoreDescendingThenDateDescending()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var source = ItemFactory.CreateItem(
            id: 1,
            type: ItemType.Lost,
            title: "Dell XPS Laptop 15",
            description: "Silver Dell laptop with charger",
            categoryId: 1,
            location: "Library Hall",
            dateLostOrFound: now);

        var highMatch = ItemFactory.CreateItem(
            id: 2,
            type: ItemType.Found,
            title: "Dell XPS Laptop 15",
            description: "Silver Dell laptop",
            categoryId: 1,
            location: "Library Hall",
            dateLostOrFound: now.AddDays(-1));

        var mediumMatch = ItemFactory.CreateItem(
            id: 3,
            type: ItemType.Found,
            title: "Dell Laptop",
            description: "Found in library",
            categoryId: 1,
            location: "Library Building",
            dateLostOrFound: now.Date.AddDays(-1).AddHours(2));

        var mediumMatchNewer = ItemFactory.CreateItem(
            id: 4,
            type: ItemType.Found,
            title: "Dell Laptop",
            description: "Found in library",
            categoryId: 1,
            location: "Library Building",
            dateLostOrFound: now.Date.AddDays(-1).AddHours(4));

        // Act
        var results = _sut.FindMatches(source, new[] { mediumMatch, highMatch, mediumMatchNewer });

        // Assert
        results.Should().HaveCount(3);
        results[0].ItemId.Should().Be(highMatch.Id);
        results[0].MatchScore.Should().BeGreaterThan(results[1].MatchScore);
        results[1].ItemId.Should().Be(mediumMatchNewer.Id);
        results[2].ItemId.Should().Be(mediumMatch.Id);
        results[1].MatchScore.Should().Be(results[2].MatchScore);
        results[1].DateLostOrFound.Should().BeAfter(results[2].DateLostOrFound);
    }
}
