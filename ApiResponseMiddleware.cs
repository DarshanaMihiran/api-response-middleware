using Newtonsoft.Json;
using System.Text;

namespace dotnetwebapi_boilerplate.Infarstructure
{
    public class ApiResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            var responseBody = new MemoryStream();
            try
            {
                context.Response.Body = responseBody;

                await _next(context);

                if (context.Response.StatusCode >= 200 && context.Response.StatusCode <= 299)
                {
                    await WriteResponseAsync(context, true);
                }
                else
                {
                    await WriteResponseAsync(context, false);
                }

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.Clear();
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var apiResponse = new ApiResponse<object>
            {
                IsSuccess = false,
                Data = null,
                Message = "An error occurred in the request.",
                StatusCode = context.Response.StatusCode,
                Timestamp = DateTime.UtcNow,
                Errors = new List<string> { exception.Message }
            };

            var serializedApiResponse = JsonConvert.SerializeObject(apiResponse);
            await context.Response.WriteAsync(serializedApiResponse, Encoding.UTF8);
        }

        private static async Task WriteResponseAsync(HttpContext context, bool success)
        {
            context.Response.ContentType = "application/json";

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            context.Response.Body.SetLength(0);

            var apiResponse = new ApiResponse<object>
            {
                IsSuccess = success,
                Data = JsonConvert.DeserializeObject(responseBody),
                Message = success ? "Request successful." : "An error occurred in the request.",
                StatusCode = context.Response.StatusCode,
                Timestamp = DateTime.UtcNow
            };

            if (context.Response.Headers.TryGetValue("X-Pagination-TotalItems", out var totalItemsValue) &&
            context.Response.Headers.TryGetValue("X-Pagination-CurrentPage", out var currentPageValue) &&
            context.Response.Headers.TryGetValue("X-Pagination-PageSize", out var pageSizeValue))
            {
                if (int.TryParse(totalItemsValue, out var totalItems) &&
                    int.TryParse(currentPageValue, out var currentPage) &&
                    int.TryParse(pageSizeValue, out var pageSize))
                {
                    apiResponse.Pagination = new PaginationInfo
                    {
                        TotalItems = totalItems,
                        CurrentPage = currentPage,
                        PageSize = pageSize
                    };
                }
            }

            if (!success)
            {
                var errorDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
                if (errorDetails != null)
                {
                    apiResponse.Errors = new List<string>(errorDetails.Values);
                }
            }
            var serializedApiResponse = JsonConvert.SerializeObject(apiResponse);
            await context.Response.WriteAsync(serializedApiResponse, Encoding.UTF8);
        }
    }
}
