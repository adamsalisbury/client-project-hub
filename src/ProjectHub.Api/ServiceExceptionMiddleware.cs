using ProjectHub.Services;

namespace ProjectHub.Api;

/// <summary>
/// Translates <see cref="ServiceException"/> instances thrown anywhere down
/// the pipeline into appropriate HTTP responses, so controllers can stay
/// transport-agnostic and just rethrow.
/// </summary>
public sealed class ServiceExceptionMiddleware(RequestDelegate next, ILogger<ServiceExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, new { error = ex.Message });
        }
        catch (UnprocessableException ex)
        {
            await WriteAsync(context, StatusCodes.Status422UnprocessableEntity, new { error = ex.Message, detail = ex.Detail });
        }
        catch (ServiceException ex)
        {
            logger.LogWarning(ex, "Unhandled service exception");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, object body)
    {
        if (context.Response.HasStarted)
        {
            return;
        }
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(body);
    }
}
