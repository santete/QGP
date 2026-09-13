using Microsoft.AspNetCore.Diagnostics;
using Qgp.Api.Application;
using Qgp.Api.Contracts;

namespace Qgp.Api.Api;

/// <summary>
/// Map exception → envelope { error: { code, message } } (openapi/ADR-0014).
/// AppException → status + code nghiệp vụ; lỗi khác → 500 INTERNAL (KHÔNG lộ stack — SECURITY_RULES).
/// </summary>
public sealed class QgpExceptionHandler(ILogger<QgpExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        ErrorResponse body;
        if (ex is AppException app)
        {
            ctx.Response.StatusCode = app.Status;
            body = new ErrorResponse(new ErrorBody(app.Code, app.Message));
        }
        else
        {
            logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = 500;
            body = new ErrorResponse(new ErrorBody("INTERNAL", "Đã xảy ra lỗi hệ thống"));
        }

        await ctx.Response.WriteAsJsonAsync(body, ct);
        return true;
    }
}
