using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.Common;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Application.Interfaces;
using MediatR;

namespace LostAndFound.Application.Features.Items.Queries.GetItemById;

public class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, Result<ItemResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetItemByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ItemResponseDto>> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Items.GetByIdWithDetailsAsync(request.Id, asNoTracking: true, cancellationToken);
        if (item is null)
        {
            return Result<ItemResponseDto>.Failure("Item not found.", ResultErrorType.NotFound);
        }

        return Result<ItemResponseDto>.Success(item.ToResponseDto());
    }
}
