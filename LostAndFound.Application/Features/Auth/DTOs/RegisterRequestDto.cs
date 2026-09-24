namespace LostAndFound.Application.Features.Auth.DTOs;

public record RegisterRequestDto(string FirstName, string LastName, string Email, string Password);
