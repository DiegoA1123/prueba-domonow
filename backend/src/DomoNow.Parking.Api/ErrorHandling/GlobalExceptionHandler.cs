using Microsoft.AspNetCore.Diagnostics;

namespace DomoNow.Parking.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Solicitud mal formada (p. ej. JSON inválido): es un error del cliente, no del servidor.
        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning("Bad request on {Method} {Path}: {Reason}",
                httpContext.Request.Method, httpContext.Request.Path, badRequest.Message);
            return await WriteProblemAsync(httpContext, exception, badRequest.StatusCode,
                "Solicitud inválida", "El cuerpo de la solicitud no es válido.");
        }

        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);
        return await WriteProblemAsync(httpContext, exception, StatusCodes.Status500InternalServerError,
            "Error inesperado", "Ocurrió un error inesperado. Intente de nuevo más tarde.");
    }

    private ValueTask<bool> WriteProblemAsync(
        HttpContext httpContext, Exception exception, int status, string title, string detail)
    {
        httpContext.Response.StatusCode = status;
        return problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Title = title, Detail = detail, Status = status },
        });
    }
}
