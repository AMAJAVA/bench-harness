using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Exceptions;

namespace TeamLeave.Api.Middleware;

public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is AppException appEx)
        {
            var problem = new ProblemDetails
            {
                Status = appEx.StatusCode,
                Title = appEx.Code,
                Detail = appEx.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            };
            if (appEx.Errors != null)
                problem.Extensions["errors"] = appEx.Errors;

            context.Result = new ObjectResult(problem) { StatusCode = appEx.StatusCode };
            context.ExceptionHandled = true;
        }
        else if (context.Exception is DbUpdateConcurrencyException)
        {
            var problem = new ProblemDetails
            {
                Status = 409,
                Title = "CONCURRENCY_CONFLICT",
                Detail = "Un conflit de concurrence est survenu. Veuillez recharger et réessayer.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            };
            context.Result = new ObjectResult(problem) { StatusCode = 409 };
            context.ExceptionHandled = true;
        }
        else
        {
            var problem = new ProblemDetails
            {
                Status = 500,
                Title = "INTERNAL_SERVER_ERROR",
                Detail = "Une erreur interne est survenue.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            context.Result = new ObjectResult(problem) { StatusCode = 500 };
            context.ExceptionHandled = true;
        }
    }
}
