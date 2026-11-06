using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Api.Controllers;

/// <summary>
/// Base API controller for all banking endpoints.
/// Standardizes route versioning (/api/v1/[controller]) and Railway-Oriented Result mapping.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Translates a domain Result into an appropriate HTTP response.
    /// Eliminates boilerplate if/else checks across all controllers.
    /// </summary>
    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return MapErrorToResponse(result.Error);
    }

    /// <summary>
    /// Translates a generic domain Result{T} into an appropriate HTTP response.
    /// </summary>
    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapErrorToResponse(result.Error);
    }

    private ActionResult MapErrorToResponse(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found",
                Detail = error.Description,
                Extensions = { ["errorCode"] = error.Code }
            }),

            ErrorType.Validation => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = error.Description,
                Extensions = { ["errorCode"] = error.Code }
            }),

            ErrorType.Conflict => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict Detected",
                Detail = error.Description,
                Extensions = { ["errorCode"] = error.Code }
            }),

            ErrorType.Unauthorized => Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = error.Description,
                Extensions = { ["errorCode"] = error.Code }
            }),

            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = error.Description,
                Extensions = { ["errorCode"] = error.Code }
            }),

            _ => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = error.Description,
                Extensions = { ["errorCode"] = error.Code }
            })
        };
    }
}
