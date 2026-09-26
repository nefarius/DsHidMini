namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Writes a diagnostic run to a single, privacy-aware ZIP file that a user can attach to a
///     support request.
/// </summary>
public interface IDiagnosticBundleWriter
{
    /// <summary>
    ///     Writes <paramref name="content" /> to <paramref name="destinationZipPath" />.
    /// </summary>
    /// <param name="content">The diagnostic run to export.</param>
    /// <param name="destinationZipPath">Full path of the ZIP file to create (overwritten if it exists).</param>
    /// <param name="redact">
    ///     When <see langword="true" /> (the default), Bluetooth addresses and PnP instance IDs are
    ///     replaced with stable per-bundle hashes so two events from the same device can still be
    ///     correlated without exposing the real address.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the write operation.</param>
    Task WriteAsync(
        DiagnosticBundleContent content,
        string destinationZipPath,
        bool redact = true,
        CancellationToken cancellationToken = default);
}
