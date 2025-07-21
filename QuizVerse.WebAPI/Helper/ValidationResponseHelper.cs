using Microsoft.AspNetCore.Mvc;
using QuizVerse.Infrastructure.ApiResponse;

namespace QuizVerse.WebAPI.Helper;

public static class ValidationResponseHelper
{
    // for returning the validation error in the APIresponse formate 
    public static IActionResult CreateValidationErrorResponse(ActionContext context, bool isDevelopment)
    {
        try
        {
            var errors = context.ModelState
                .Where(ms => ms.Value.Errors.Any())
                .Select(ms => new
                {
                    Field = isDevelopment ? ms.Key : null,
                    Errors = ms.Value.Errors.Select(e => isDevelopment ? e.ErrorMessage : "Invalid input.")
                });

            return new BadRequestObjectResult(new ApiResponse<object>
            {
                Result = false,
                Message = "Validation failed.",
                StatusCode = 400,
                Data = errors
            });
        }
        catch
        {
            return new BadRequestObjectResult(new ApiResponse<object>
            {
                Result = false,
                Message = "Invalid request.",
                StatusCode = 400,
                Data = null
            });
        }
    }
}

