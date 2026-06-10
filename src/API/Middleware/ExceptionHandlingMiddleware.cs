using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Responses;

namespace ProductManagement.API.Middleware
{
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
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred.";
            var errors = new List<string>();

            switch (exception)
            {
                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = notFoundException.Message;
                    break;
                case AuthenticationException authException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = authException.Message;
                    break;
                case BadHttpRequestException badRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = badRequestException.Message;
                    break;
                case FluentValidation.ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Validation failed.";
                    errors.AddRange(validationException.Errors.Select(x => x.ErrorMessage));
                    break;
                // Add more custom exceptions here if needed
                default:
                    if (_env.IsDevelopment())
                    {
                        message = exception.Message;
                        errors.Add(exception.StackTrace ?? "");
                    }
                    break;
            }

            context.Response.StatusCode = (int)statusCode;
            var response = ApiResponse<object>.Failure(message, errors);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
