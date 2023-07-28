using Newtonsoft.Json;

namespace dotnetwebapi_boilerplate.Infarstructure
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
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
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var errorResponse = new ApiResponse<object>
            {
                IsSuccess = false,
                Data = null,
                Message = "An error occurred in the request.",
                StatusCode = context.Response.StatusCode,
                Timestamp = DateTime.UtcNow,
                Errors = new List<string> { exception.Message }
            };

            var serializedErrorResponse = JsonConvert.SerializeObject(errorResponse);
            await context.Response.WriteAsync(serializedErrorResponse);
        }
    }
}
