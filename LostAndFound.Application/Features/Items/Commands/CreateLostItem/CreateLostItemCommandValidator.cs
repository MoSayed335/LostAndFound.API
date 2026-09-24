using LostAndFound.Application.Features.Items.Commands.Common;
using LostAndFound.Application.Interfaces;

namespace LostAndFound.Application.Features.Items.Commands.CreateLostItem;

public class CreateLostItemCommandValidator : CreateItemCommandValidator<CreateLostItemCommand>
{
    public CreateLostItemCommandValidator(ICategoryRepository categoryRepository)
        : base(categoryRepository)
    {
    }
}
