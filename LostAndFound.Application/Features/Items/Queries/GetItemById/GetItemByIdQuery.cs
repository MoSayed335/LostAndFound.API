using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Items.Queries.GetItemById;

public record GetItemByIdQuery(int Id) : IRequest<Result<ItemResponseDto>>;
