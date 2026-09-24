using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using Moq;

namespace LostAndFound.Tests.Common;

public static class UserFactory
{
    public static ApplicationUser CreateUser(
        int id = 1,
        string email = "user1@lostandfound.dev",
        string firstName = "John",
        string lastName = "Doe")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public static class CategoryFactory
{
    public static Category CreateCategory(int id = 1, string name = "Electronics")
    {
        return new Category
        {
            Id = id,
            Name = name,
            Description = $"{name} items"
        };
    }
}

public static class ItemFactory
{
    public static Item CreateItem(
        int id = 1,
        string title = "Black Leather Wallet",
        string description = "Contains cards and driver's license",
        ItemType type = ItemType.Lost,
        int categoryId = 1,
        int userId = 1,
        string location = "Central Library 2nd Floor",
        DateTime? dateLostOrFound = null,
        ItemStatus status = ItemStatus.Active,
        string? imageUrl = null)
    {
        return new Item
        {
            Id = id,
            Title = title,
            Description = description,
            Type = type,
            CategoryId = categoryId,
            Category = CategoryFactory.CreateCategory(categoryId),
            UserId = userId,
            User = UserFactory.CreateUser(userId),
            Location = location,
            DateLostOrFound = dateLostOrFound ?? DateTime.UtcNow.AddDays(-2),
            Status = status,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = null
        };
    }
}

public static class ClaimFactory
{
    public static Claim CreateClaim(
        int id = 1,
        int itemId = 1,
        int claimantId = 2,
        string message = "This matches the wallet I lost yesterday near the library.",
        ClaimStatus status = ClaimStatus.Pending,
        Item? item = null)
    {
        return new Claim
        {
            Id = id,
            ItemId = itemId,
            Item = item ?? ItemFactory.CreateItem(itemId, userId: 1),
            ClaimantId = claimantId,
            Claimant = UserFactory.CreateUser(claimantId, email: $"claimant{claimantId}@lostandfound.dev"),
            Message = message,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddHours(-5),
            ReviewedAt = null
        };
    }
}

public static class MockServiceFactory
{
    public static Mock<ICurrentUserService> CreateCurrentUserService(int? userId = 1, bool isAdmin = false)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(s => s.UserId).Returns(userId);
        mock.Setup(s => s.IsAdmin).Returns(isAdmin);
        mock.Setup(s => s.IsAuthenticated).Returns(userId.HasValue);
        return mock;
    }

    public static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return mock;
    }

    public static (Mock<IUnitOfWork> UowMock, Mock<IItemRepository> ItemRepoMock, Mock<IClaimRepository> ClaimRepoMock, Mock<ICategoryRepository> CategoryRepoMock) CreateMockUnitOfWork()
    {
        var uowMock = new Mock<IUnitOfWork>();
        var itemRepoMock = new Mock<IItemRepository>();
        var claimRepoMock = new Mock<IClaimRepository>();
        var categoryRepoMock = new Mock<ICategoryRepository>();

        uowMock.Setup(u => u.Items).Returns(itemRepoMock.Object);
        uowMock.Setup(u => u.Claims).Returns(claimRepoMock.Object);
        uowMock.Setup(u => u.Categories).Returns(categoryRepoMock.Object);
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return (uowMock, itemRepoMock, claimRepoMock, categoryRepoMock);
    }
}
