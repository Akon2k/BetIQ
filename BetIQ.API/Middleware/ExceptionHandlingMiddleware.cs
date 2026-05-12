using System.Net;
using System.Text.Json;

namespace BetIQ.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Solo exponer detalles de error en Development para evitar Information Disclosure
        object response;
        if (_env.IsDevelopment())
        {
            response = new
            {
                Message = "Internal Server Error",
                Detailed = exception.Message,
                InnerException = exception.InnerException?.Message
            };
        }
        else
        {
            response = new { Message = "Ha ocurrido un error interno en el servidor." };
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);

        return context.Response.WriteAsync(jsonResponse);
    }
}

