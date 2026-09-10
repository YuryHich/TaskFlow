using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.ExceptionHandling
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
                UserAlreadyExistsException => (StatusCodes.Status409Conflict, "Conflict"),
                UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                ValidationException => (StatusCodes.Status400BadRequest, "Bad Request"),
                _ => (0, string.Empty)
            };

            if (statusCode == 0) return false;

            httpContext.Response.StatusCode = statusCode;

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}