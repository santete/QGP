namespace Qgp.Api.Application;

/// <summary>
/// Lỗi nghiệp vụ có mã (ISC Error Code catalog, UPPER_SNAKE) + HTTP status.
/// Controller/middleware map thành envelope { error: { code, message } } (openapi/ADR-0014).
/// </summary>
public sealed class AppException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;

    public static AppException NotFound(string code, string message) => new(404, code, message);
    public static AppException Conflict(string code, string message) => new(409, code, message);
    public static AppException Validation(string message) => new(422, "VALIDATION_ERROR", message);
    public static AppException StateConflict(string message) => new(409, "STATE_CONFLICT", message);
}
