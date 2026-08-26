namespace StockFlow.Application.Models;

public sealed record UseCaseResult<T>(
    int StatusCode,
    T? Data = default,
    string? Message = null,
    string? Location = null)
{
    public static UseCaseResult<T> Ok(T data) => new(200, data);

    public static UseCaseResult<T> Created(T data, string location) =>
        new(201, data, Location: location);

    public static UseCaseResult<T> BadRequest(string message) =>
        new(400, Message: message);

    public static UseCaseResult<T> Unauthorized(string message) =>
        new(401, Message: message);

    public static UseCaseResult<T> NotFound(string message) =>
        new(404, Message: message);
}

public sealed record UseCaseResult(int StatusCode, string? Message = null)
{
    public static UseCaseResult NoContent() => new(204);

    public static UseCaseResult NotFound(string message) =>
        new(404, message);
}
