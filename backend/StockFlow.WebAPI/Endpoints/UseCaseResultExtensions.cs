using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public static class UseCaseResultExtensions
{
    public static IResult ToHttpResult<T>(this UseCaseResult<T> result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Results.Ok(result.Data),
            StatusCodes.Status201Created => Results.Created(result.Location, result.Data),
            StatusCodes.Status400BadRequest => Results.BadRequest(new { message = result.Message }),
            StatusCodes.Status401Unauthorized => Results.Json(
                new
                {
                    message = result.Message
                },
                statusCode: StatusCodes.Status401Unauthorized),
            StatusCodes.Status404NotFound => Results.NotFound(new { message = result.Message }),
            _ => Results.Json(
                new
                {
                    message = result.Message ?? "Terjadi kesalahan. Silakan coba kembali."
                },
                statusCode: result.StatusCode)
        };
    }

    public static IResult ToHttpResult(this UseCaseResult result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status204NoContent => Results.NoContent(),
            StatusCodes.Status404NotFound => Results.NotFound(new { message = result.Message }),
            _ => Results.Json(
                new
                {
                    message = result.Message ?? "Terjadi kesalahan. Silakan coba kembali."
                },
                statusCode: result.StatusCode)
        };
    }
}
