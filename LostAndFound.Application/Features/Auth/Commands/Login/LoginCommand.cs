using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Auth.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponseDto>>;
