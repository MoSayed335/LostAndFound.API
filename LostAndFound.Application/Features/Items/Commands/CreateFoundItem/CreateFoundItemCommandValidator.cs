using LostAndFound.Application.Features.Items.Commands.Common;
using LostAndFound.Application.Interfaces;

namespace LostAndFound.Application.Features.Items.Commands.CreateFoundItem;

public class CreateFoundItemCommandValidator : CreateItemCommandValidator<CreateFoundItemCommand>
{
    public CreateFoundItemCommandValidator(ICategoryRepository categoryRepository)
        : base(categoryRepository)
    {
    }
}
