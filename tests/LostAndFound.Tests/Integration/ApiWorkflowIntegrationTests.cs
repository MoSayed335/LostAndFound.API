using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LostAndFound.API;
using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Auth.DTOs;
using LostAndFound.Application.Features.Items.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LostAndFound.Tests.Integration;

public class ApiWorkflowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiWorkflowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetItems_WithoutAuthentication_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/items?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PaginatedList<ItemResponseDto>>(JsonOptions);
        pagedList.Should().NotBeNull();
        pagedList!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateLostItem_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateItemRequest(
            Title: "Lost Laptop",
            Description: "Black ThinkPad",
            CategoryId: 1,
            Location: "Library",
            DateLostOrFound: DateTime.UtcNow
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/items/lost", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullWorkflow_Register_Login_Me_And_CreateItem()
    {
        // 1. Register new user
        var uniqueEmail = $"workflow_{Guid.NewGuid():N}@integration.test";
        var password = "SecurePassword@123";
        var registerRequest = new RegisterRequestDto(
            Email: uniqueEmail,
            Password: password,
            FirstName: "Workflow",
            LastName: "Tester"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var regData = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        regData.Should().NotBeNull();
        regData!.AccessToken.Should().NotBeNullOrWhiteSpace();

        // 2. Login
        var loginRequest = new LoginRequestDto(
            Email: uniqueEmail,
            Password: password
        );

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        loginData.Should().NotBeNull();
        var token = loginData!.AccessToken;

        // 3. Authenticate HTTP client with Bearer token
        using var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 4. Access /api/auth/me
        var meResponse = await authClient.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Create a Lost Item
        var createItemRequest = new CreateItemRequest(
            Title: "Workflow Silver MacBook",
            Description: "MacBook Pro 14 with blue sticker",
            CategoryId: 1, // Electronics category from seed data
            Location: "Building 3 Room 101",
            DateLostOrFound: DateTime.UtcNow.AddDays(-1)
        );

        var createItemResponse = await authClient.PostAsJsonAsync("/api/items/lost", createItemRequest);
        createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdItem = await createItemResponse.Content.ReadFromJsonAsync<ItemResponseDto>(JsonOptions);
        createdItem.Should().NotBeNull();
        createdItem!.Title.Should().Be("Workflow Silver MacBook");
        createdItem.Id.Should().BeGreaterThan(0);

        // 6. Retrieve the created item by ID publicly
        var getItemResponse = await _client.GetAsync($"/api/items/{createdItem.Id}");
        getItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedItem = await getItemResponse.Content.ReadFromJsonAsync<ItemResponseDto>(JsonOptions);
        retrievedItem.Should().NotBeNull();
        retrievedItem!.Title.Should().Be("Workflow Silver MacBook");
        retrievedItem.Owner.Should().NotBeNull();
        retrievedItem.Owner!.Email.Should().Be(uniqueEmail);
    }
}
