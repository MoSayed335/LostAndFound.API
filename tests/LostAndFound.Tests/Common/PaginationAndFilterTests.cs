using FluentAssertions;
using LostAndFound.Application.Common.Models;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using LostAndFound.Tests.Common;
using Xunit;

namespace LostAndFound.Tests.Common;

public class PaginationAndFilterTests
{
    [Fact]
    public void PaginatedList_WhenFirstPageOfMultiple_ShouldCalculateFlagsCorrectly()
    {
        var items = new List<string> { "Item 1", "Item 2" };
        var list = new PaginatedList<string>(items, totalCount: 25, pageNumber: 1, pageSize: 10);

        list.TotalCount.Should().Be(25);
        list.PageNumber.Should().Be(1);
        list.TotalPages.Should().Be(3);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedList_WhenMiddlePage_ShouldHaveBothPreviousAndNextPage()
    {
        var items = new List<string> { "Item 11", "Item 12" };
        var list = new PaginatedList<string>(items, totalCount: 25, pageNumber: 2, pageSize: 10);

        list.TotalPages.Should().Be(3);
        list.HasPreviousPage.Should().BeTrue();
        list.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedList_WhenLastPage_ShouldHavePreviousButNotNextPage()
    {
        var items = new List<string> { "Item 21" };
        var list = new PaginatedList<string>(items, totalCount: 25, pageNumber: 3, pageSize: 10);

        list.HasPreviousPage.Should().Be(true);
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedList_WhenSinglePage_ShouldHaveNeitherPreviousNorNextPage()
    {
        var items = new List<string> { "A", "B", "C" };
        var list = new PaginatedList<string>(items, totalCount: 3, pageNumber: 1, pageSize: 10);

        list.TotalPages.Should().Be(1);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedList_WhenEmpty_ShouldHaveZeroTotalPages()
    {
        var list = new PaginatedList<string>(new List<string>(), totalCount: 0, pageNumber: 1, pageSize: 10);

        list.TotalPages.Should().Be(0);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginationRequest_WhenPageSizeExceedsMaximum_ShouldClampToFifty()
    {
        var request = new PaginationRequest { PageSize = 100 };

        request.PageSize.Should().Be(50);
    }

    [Fact]
    public void PaginationRequest_WhenPageSizeIsNegativeOrZero_ShouldClampToDefaultTen()
    {
        var requestZero = new PaginationRequest { PageSize = 0 };
        var requestNeg = new PaginationRequest { PageSize = -5 };

        requestZero.PageSize.Should().Be(10);
        requestNeg.PageSize.Should().Be(10);
    }

    [Fact]
    public void PaginationRequest_WhenPageNumberIsLessThanOne_ShouldClampToOne()
    {
        var request = new PaginationRequest { PageNumber = -2 };

        request.PageNumber.Should().Be(1);
    }

    [Fact]
    public void FilterLogic_ShouldFilterAcrossTypeCategoryStatusDateAndSearch()
    {
        // Arrange
        var date1 = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);

        var items = new List<Item>
        {
            ItemFactory.CreateItem(id: 1, title: "Blue Backpack", description: "Contains textbooks", type: ItemType.Lost, categoryId: 1, location: "Library", dateLostOrFound: date1, status: ItemStatus.Active),
            ItemFactory.CreateItem(id: 2, title: "Red Backpack", description: "Gym clothes inside", type: ItemType.Found, categoryId: 1, location: "Gym", dateLostOrFound: date2, status: ItemStatus.Active),
            ItemFactory.CreateItem(id: 3, title: "iPhone 13", description: "Black silicone case", type: ItemType.Lost, categoryId: 2, location: "Library", dateLostOrFound: date2, status: ItemStatus.Claimed),
            ItemFactory.CreateItem(id: 4, title: "Car Keys", description: "Toyota key fob", type: ItemType.Lost, categoryId: 3, location: "Parking Lot B", dateLostOrFound: date1, status: ItemStatus.Returned)
        }.AsQueryable();

        // 1. Filter by Type
        var lostItems = items.Where(i => i.Type == ItemType.Lost).ToList();
        lostItems.Should().HaveCount(3);

        // 2. Filter by Category
        var cat1Items = items.Where(i => i.CategoryId == 1).ToList();
        cat1Items.Should().HaveCount(2);

        // 3. Filter by Status
        var activeItems = items.Where(i => i.Status == ItemStatus.Active).ToList();
        activeItems.Should().HaveCount(2);

        // 4. Filter by Date range
        var midDateItems = items.Where(i => i.DateLostOrFound >= new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)).ToList();
        midDateItems.Should().HaveCount(2);

        // 5. Search across Title, Description, and Location
        var searchLibrary = "library";
        var libraryResults = items.Where(i =>
            i.Title.ToLower().Contains(searchLibrary) ||
            i.Description.ToLower().Contains(searchLibrary) ||
            i.Location.ToLower().Contains(searchLibrary)).ToList();
        libraryResults.Should().HaveCount(2);

        var searchGym = "gym";
        var gymResults = items.Where(i =>
            i.Title.ToLower().Contains(searchGym) ||
            i.Description.ToLower().Contains(searchGym) ||
            i.Location.ToLower().Contains(searchGym)).ToList();
        gymResults.Should().HaveCount(1);
        gymResults[0].Id.Should().Be(2);
    }
}
