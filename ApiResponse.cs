namespace dotnetwebapi_boilerplate.Infarstructure
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public PaginationInfo? Pagination { get; set; }
        public DateTime Timestamp { get; set; }
        public string? RequestId { get; set; }
        public object? Meta { get; set; }

    }
}
