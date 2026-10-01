using System.Diagnostics;
using System.IO;
using System.Reflection;

using Microsoft.Win32;

using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.IPC;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

/// <summary>
///     Drives the guided controller diagnostic export: driver sweep, 15-second live capture, save dialog.
/// </summary>
public sealed partial class ControllerDiagnosticExportViewModel : ObservableObject
{
    private readonly AppSnackbarMessagesService _snackbar;
    private readonly IControllerDiagnosticBundleWriter _writer;
    private readonly ControllerExportRequest _request;
    private CancellationTokenSource? _cts;

    public ControllerDiagnosticExportViewModel(
        ControllerExportRequest request,
        AppSnackbarMessagesService snackbar,
        IControllerDiagnosticBundleWriter? writer = null)
    {
        _request = request;
        _snackbar = snackbar;
        _writer = writer ?? new ControllerDiagnosticBundleWriter();
        Title = $"Export diagnostics — {request.DeviceTitle}";
        CanCapture = request.SlotIndex is not null;
        IncludeLiveCapture = CanCapture;
        StatusText = request.IsWireless
            ? "This controller is connected via Bluetooth. The export will contain cached identification data" +
              (CanCapture ? " and a live motion capture." : ".") +
              " Connect it via USB for the complete probe."
            : "Ready. The export reads the controller's descriptors and feature reports and records 15 seconds of motion.";
    }

    public string Title { get; }

    public bool CanCapture { get; }

    [ObservableProperty]
    private bool _redact = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    private bool _includeLiveCapture;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(CanChangeOptions))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _instructionText = "";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExport))]
    private string? _lastExportPath;

    public bool HasExport => LastExportPath is not null;

    public bool CanStart => !IsRunning;

    public bool CanChangeOptions => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        IsRunning = true;
        ProgressValue = 0;
        LastExportPath = null;

        try
        {
            StatusText = "Reading the controller…";
            InstructionText = "Do not unplug the controller.";

            ControllerSweepOutcome sweep = await ControllerDiagnosticCollector.CollectSweepAsync(
                _request,
                DsHidMiniInterop.IsAvailable,
                slot =>
                {
                    using DsHidMiniInterop interop = new();
                    return interop.CollectControllerDiagnostics(slot);
                });

            token.ThrowIfCancellationRequested();

            ControllerTelemetryCapture? telemetry = null;
            if (IncludeLiveCapture && _request.SlotIndex is int slotIndex && DsHidMiniInterop.IsAvailable)
            {
                telemetry = await RunCaptureAsync(slotIndex, token);
            }

            token.ThrowIfCancellationRequested();

            StatusText = "Choose where to save the export.";
            InstructionText = string.Empty;

            SaveFileDialog dialog = new()
            {
                FileName = $"ControllerDiagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
                Filter = "ZIP archive (*.zip)|*.zip",
                DefaultExt = ".zip"
            };

            if (dialog.ShowDialog() != true)
            {
                StatusText = "Export cancelled. Nothing was saved.";
                return;
            }

            ControllerDiagnosticContent content = ControllerDiagnosticCollector.BuildContent(
                _request,
                Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                sweep,
                telemetry);

            await _writer.WriteAsync(content, dialog.FileName, Redact, token);

            LastExportPath = dialog.FileName;
            string? note = string.Join(
                " ",
                new[] { sweep.Note, telemetry?.Note }.Where(n => !string.IsNullOrEmpty(n)));
            bool partial = note.Length > 0;
            StatusText = partial
                ? $"Saved {dialog.FileName} (partial: {note})"
                : $"Saved {dialog.FileName}";
            _snackbar.ShowControllerDiagnosticsExportedMessage(dialog.FileName, partial);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Export cancelled. Nothing was saved.";
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Controller diagnostic export failed for '{Device}'.", _request.DeviceTitle);
            StatusText = $"Export failed: {ex.Message}";
            _snackbar.ShowControllerDiagnosticsExportFailedMessage(ex.Message);
        }
        finally
        {
            IsRunning = false;
            ProgressValue = 0;
            InstructionText = string.Empty;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task<ControllerTelemetryCapture> RunCaptureAsync(int slotIndex, CancellationToken token)
    {
        StatusText = "Recording motion…";
        Progress<GuidedCaptureProgress> progress = new(p =>
        {
            InstructionText = $"{p.Phase.Name}: {p.Phase.Instruction}";
            ProgressValue = p.Total > TimeSpan.Zero
                ? Math.Clamp(p.Elapsed.TotalSeconds / p.Total.TotalSeconds * 100.0, 0, 100)
                : 0;
        });

        try
        {
            using DsHidMiniTelemetrySource source = new(slotIndex);
            return await GuidedCaptureRunner.RunAsync(
                source, GuidedCaptureRunner.DefaultPhases, progress, token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Guided capture failed; exporting without live telemetry.");
            return new ControllerTelemetryCapture
            {
                MotionAvailable = false,
                Note = $"The live capture could not run: {ex.Message}"
            };
        }
    }

    public void RequestCancel()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestCancel();
    }

    [RelayCommand]
    private void ShowInFolder()
    {
        if (LastExportPath is { } path && File.Exists(path))
        {
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
    }
}
