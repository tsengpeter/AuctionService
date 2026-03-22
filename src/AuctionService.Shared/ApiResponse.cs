namespace AuctionService.Shared;

public record ApiResponse<T>(bool Success, T? Data, int StatusCode);

public record ApiResponse(bool Success, IEnumerable<FieldError>? Errors, string? Error, int StatusCode);

public record FieldError(string Field, string Message);

public static class ApiResponseFactory
{
    public static ApiResponse<T> Ok<T>(T data, int statusCode = 200)
        => new(true, data, statusCode);

    public static ApiResponse Fail(string error, int statusCode)
        => new(false, null, error, statusCode);

    public static ApiResponse ValidationFail(IEnumerable<FieldError> errors)
        => new(false, errors, null, 422);
}
