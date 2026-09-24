using LostAndFound.Application.Common.Models;
using MediatR;

namespace LostAndFound.Application.Features.Items.Commands.DeleteItem;

public record DeleteItemCommand(int Id, int CurrentUserId, bool IsAdmin) : IRequest<Result<bool>>;
