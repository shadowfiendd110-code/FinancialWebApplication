using Microsoft.AspNetCore.Mvc;
using WebApplication3.Exceptions;

namespace WebApplication3.Middlewares
{
    /// <summary>
    /// Обработчик исключений.
    /// </summary>
    public class ExceptionMiddleware
    {
        /// <summary>
        /// Следующий middleware.
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// Инициализация обработчика исключений.
        /// </summary>
        /// <param name="next">Следующий middleware.</param>
        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Вызывает следующий middleware.
        /// </summary>
        /// <param name="context">Http контекст.</param>
        public async Task Invoke(HttpContext context)
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

        /// <summary>
        /// Обрабатывает исключения.
        /// </summary>
        /// <param name="context">Контекст</param>
        /// <param name="exception">Исключение.</param>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int statusCode;
            string message;

            if (exception is ApiException apiEx)
            {
                statusCode = apiEx.StatusCode;
                message = apiEx.Message;
            }
            else
            {
                statusCode = StatusCodes.Status500InternalServerError;
                message = "Internal server error";
            }

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = message,
                Detail = exception is ApiException ? null : exception.Message,
                Instance = context.Request.Path,
                Type = "https://httpstatuses.io/" + statusCode
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
