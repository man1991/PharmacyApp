using System.Net;
using System.Text.Json;
using PharmacyApp.Api.Exceptions;

namespace PharmacyApp.Api.Middleware
{
    /// <summary>
    /// Central place where every unhandled exception in the pipeline is caught,
    /// logged, and converted into a consistent, user-friendly JSON error response.
    /// This means controllers/services can simply "throw" and never have to worry
    /// about formatting error responses themselves.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Defaults for anything we didn't specifically anticipate.
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var userMessage = "Something went wrong on our end. Please try again in a moment.";

            switch (exception)
            {
                // Known, "expected" application errors carry their own status code and
                // a message that is already safe/appropriate to show to the user.
                case AppException appException:
                    statusCode = appException.StatusCode;
                    userMessage = appException.Message;
                    _logger.LogWarning(exception, "Handled application exception: {Message}", exception.Message);
                    break;

                // Thrown by JSON (de)serialization when the request body / data file is malformed.
                case JsonException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    userMessage = "The data you submitted could not be understood. Please check the values and try again.";
                    _logger.LogWarning(exception, "JSON processing error");
                    break;

                // File system problems while reading/writing the JSON data store.
                case IOException or UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    userMessage = "We couldn't access the medicine records right now. Please try again shortly.";
                    _logger.LogError(exception, "Storage I/O error");
                    break;

                // Anything else is unexpected: log the full detail, but never leak
                // internal exception details/stack traces to the end user.
                default:
                    _logger.LogError(exception, "Unhandled exception");
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var payload = new
            {
                success = false,
                error = userMessage,
                // A correlation id makes it possible to find the matching log entry
                // without exposing any sensitive internal detail to the client.
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }

    /// <summary>Convenience extension so Program.cs can register the middleware in one line.</summary>
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAppExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
