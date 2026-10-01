namespace MobiFlux.Shared.Common;

public sealed record ApiResponse<T>(bool Succeeded, T? Data, ApiError? Error, string CorrelationId)
{
    public static ApiResponse<T> Ok(T data, string correlationId) => new(true, data, null, correlationId);
    public static ApiResponse<T> Fail(ApiError error, string correlationId) => new(false, default, error, correlationId);
}

public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
