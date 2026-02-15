namespace InvoiceApi.DTO;

/// <summary>
///     Extension methods for creating ApiResponse instances.
/// </summary>
public static class ResultHelper
{
    /// <summary>
    ///     Creates a success response with data.
    /// </summary>
    /// <returns></returns>
    public static ApiResponse<T> Success<T>(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
        };
    }

    /// <summary>
    ///     Creates a success response for nullable value types.
    /// </summary>
    /// <returns></returns>
    public static ApiResponse<T?> Success<T>(T? data, string? message = null)
        where T : struct
    {
        return new ApiResponse<T?>
        {
            Success = true,
            Data = data,
            Message = message,
        };
    }

    /// <summary>
    ///     Creates a success response without data.
    /// </summary>
    /// <returns></returns>
    public static ApiResponse<T> Success<T>(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = default,
            Message = message,
        };
    }

    /// <summary>
    ///     Creates a failure response for BadRequest.
    /// </summary>
    /// <returns></returns>
    public static ApiResponse<object> BadRequest(string? message = null)
    {
        return new ApiResponse<object>
        {
            Success = false,
            Message = message,
            ErrorCode = "BAD_REQUEST",
        };
    }

    /// <summary>
    ///     Creates a failure response from an unexpected exception.
    /// </summary>
    /// <returns></returns>
    public static ApiResponse<object> Error(string message, string code = "SERVER_ERROR")
    {
        return new ApiResponse<object>
        {
            Success = false,
            Message = message,
            ErrorCode = code,
        };
    }
}