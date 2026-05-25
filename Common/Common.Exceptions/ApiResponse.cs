namespace Common.Exceptions;

/// <summary>
/// Standardized API response wrapper.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();

    public ApiResponse() { }

    public ApiResponse(T data, string message = "Success")
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public ApiResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        => new(data, message);

    public static ApiResponse<T> ErrorResponse(string message, IDictionary<string, string[]>? errors = null)
    {
        var response = new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new Dictionary<string, string[]>()
        };
        return response;
    }
}

/// <summary>
/// Standardized API response wrapper for operations without data return.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();

    public ApiResponse() { }

    public ApiResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static ApiResponse SuccessResponse(string message = "Success")
        => new(true, message);

    public static ApiResponse ErrorResponse(string message, IDictionary<string, string[]>? errors = null)
    {
        var response = new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new Dictionary<string, string[]>()
        };
        return response;
    }
}
