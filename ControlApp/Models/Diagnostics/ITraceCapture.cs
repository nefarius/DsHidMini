namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Captures structured driver ETW events for the duration of a diagnostic run. Implementations
///     may be backed by a live ETW session (see <see cref="EtwDiagnosticTraceCapture" />) or by a
///     fixed replay list in tests.
/// </summary>
public interface ITraceCapture : IAsyncDisposable
{
    /// <summary>
    ///     Raised on a background thread for every decoded event while the capture is running.
    /// </summary>
    event Action<DiagnosticEventRecord>? EventCaptured;

    /// <summary>
    ///     Raised if the capture pump terminates unexpectedly (e.g. the session was stopped
    ///     externally, or a decode error occurred).
    /// </summary>
    event Action<Exception>? CaptureFaulted;

    /// <summary>
    ///     <see langword="true" /> while a capture session is active.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    ///     Starts (or restarts, after cleaning up any orphaned session) the trace capture. Requires
    ///     administrator privileges; callers must ensure elevation before calling this.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stops the capture and releases the underlying ETW session, if any.
    /// </summary>
    Task StopAsync();
}
