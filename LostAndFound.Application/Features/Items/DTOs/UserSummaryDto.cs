namespace LostAndFound.Application.Features.Items.DTOs;

public record UserSummaryDto(
    int Id,
    string FirstName,
    string LastName,
    string Email
);
