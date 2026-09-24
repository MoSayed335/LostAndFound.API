using LostAndFound.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Succeeded)
        {
            return successStatusCode switch
            {
                StatusCodes.Status201Created => StatusCode(StatusCodes.Status201Created, result.Value),
                StatusCodes.Status204NoContent => NoContent(),
                _ => Ok(result.Value)
            };
        }

        return MapError(result);
    }

    protected IActionResult MapError<T>(Result<T> result) => result.ErrorType switch
    {
        ResultErrorType.Conflict => Conflict(Envelope(result.Error!)),
        ResultErrorType.Unauthorized => Unauthorized(Envelope(result.Error!)),
        ResultErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, Envelope(result.Error!)),
        ResultErrorType.NotFound => NotFound(Envelope(result.Error!)),
        _ => BadRequest(Envelope(result.Error!))
    };

    protected static object Envelope(string message) => new
    {
        success = false,
        message,
        errors = Array.Empty<object>()
    };
}
