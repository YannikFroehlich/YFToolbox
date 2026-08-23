using YFToolbox.Core.Models;

namespace YFToolbox.Core.Errors;

public sealed class AppOperationException(
    AppErrorCode code,
    string message,
    bool isSkipped = false,
    Exception? innerException = null) : Exception(message, innerException)
{
    public AppErrorCode Code { get; } = code;

    public bool IsSkipped { get; } = isSkipped;
}
