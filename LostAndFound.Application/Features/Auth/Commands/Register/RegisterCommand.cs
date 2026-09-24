using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Auth.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string FirstName, string LastName, string Email, string Password)
    : IRequest<Result<AuthResponseDto>>;
