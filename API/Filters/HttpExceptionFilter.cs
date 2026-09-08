using Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters;

public class HttpExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case NotFoundException ex:
                context.Result = new NotFoundObjectResult(new { message = ex.Message });
                context.ExceptionHandled = true;
                break;
            case ForbiddenException ex:
                context.Result = new ObjectResult(new { message = ex.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
