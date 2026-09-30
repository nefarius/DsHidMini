using System.ComponentModel;
using System.Diagnostics;
using DualController.Core;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace DualController.Bridge;

internal sealed record ControllerStatus(string Name, string Connection, string Status,
    string Battery, string Input);

internal sealed class ControllerSession(HidDevice device) : IAsyncDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private Task? task;
    private int motors;
    private ControllerStatus status = new($"PS4 ({device.Product:X4})",
        device.Bluetooth ? "Bluetooth" : "USB", "Connecting…", "—", "—");
    public ControllerStatus Status => Volatile.Read(ref status);
    public bool Completed => task?.IsCompleted == true;
    public void Start() => task = Task.Run(RunAsync);

    private async Task RunAsync()
    {
        try
        {
            using var handle = HidDevices.Open(device.Path);
            if (handle.IsInvalid) throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            if (device.Bluetooth) HidDevices.EnableBluetoothReports(handle, device.FeatureLength);
            using var stream = new FileStream(handle, FileAccess.ReadWrite, device.InputLength, true);
            using var client = new ViGEmClient();
            var target = client.CreateXbox360Controller();
            target.AutoSubmitReport = false;
            void Feedback(object sender, Xbox360FeedbackReceivedEventArgs args) =>
                Interlocked.Exchange(ref motors, args.LargeMotor | (args.SmallMotor << 8));
            target.FeedbackReceived += Feedback;
            using var outputLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            bool connected = false;
            Task? output = null;
            try
            {
                target.Connect(); connected = true;
                output = Task.Run(() => OutputAsync(handle, outputLifetime.Token));
                byte[] buffer = new byte[device.InputLength];
                long lastValid = Stopwatch.GetTimestamp();
                while (!lifetime.IsCancellationRequested)
                {
                    using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                    readTimeout.CancelAfter(TimeSpan.FromSeconds(2));
                    int length;
                    try { length = await stream.ReadAsync(buffer, readTimeout.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
                    { throw new IOException("Input stalled; reconnecting the controller."); }
                    if (length == 0) throw new IOException("Controller disconnected.");
                    if (!Ds4Protocol.TryParse(buffer.AsSpan(0, length), device.Bluetooth, out GamepadState input))
                    {
                        if (Stopwatch.GetElapsedTime(lastValid) > TimeSpan.FromSeconds(2))
                            throw new IOException("No valid PS4 reports; reconnecting.");
                        continue;
                    }
                    lastValid = Stopwatch.GetTimestamp();
                    // Submit one complete state per report, so two physical
                    // controllers never share state or inherit stale buttons.
                    target.SetButtonsFull(input.Buttons);
                    target.SetAxisValue(Xbox360Axis.LeftThumbX, input.LeftX);
                    target.SetAxisValue(Xbox360Axis.LeftThumbY, input.LeftY);
                    target.SetAxisValue(Xbox360Axis.RightThumbX, input.RightX);
                    target.SetAxisValue(Xbox360Axis.RightThumbY, input.RightY);
                    target.SetSliderValue(Xbox360Slider.LeftTrigger, input.LeftTrigger);
                    target.SetSliderValue(Xbox360Slider.RightTrigger, input.RightTrigger);
                    target.SubmitReport();
                    Volatile.Write(ref status, new(Status.Name, Status.Connection, "Xbox input active",
                        input.BatteryPercent is int percent ? $"{percent}%{(input.Charging ? " charging" : "")}" : "—",
                        $"Buttons {input.Buttons:X4} · L2 {input.LeftTrigger} · R2 {input.RightTrigger}"));
                }
            }
            finally
            {
                outputLifetime.Cancel();
                if (output is not null) await output.ConfigureAwait(false);
                target.FeedbackReceived -= Feedback;
                if (connected)
                {
                    try { target.ResetReport(); target.SubmitReport(); }
                    finally { target.Disconnect(); }
                }
                (target as IDisposable)?.Dispose();
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Log.Write($"{Status.Name} {Status.Connection}: {ex}");
            Volatile.Write(ref status, Status with { Status = ex.Message });
        }
    }

    private async Task OutputAsync(Microsoft.Win32.SafeHandles.SafeFileHandle handle, CancellationToken token)
    {
        int sent = -1;
        using var tick = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
        try
        {
            do
            {
                int current = Volatile.Read(ref motors);
                if (current == sent) continue;
                byte[] report = Ds4Protocol.Output(device.Bluetooth, (byte)current,
                    (byte)(current >> 8), 30, 100, 255);
                if (HidDevices.WriteOutput(handle, report)) sent = current;
                else if (sent == -1)
                {
                    // Input remains usable when a third-party controller does
                    // not accept Sony's output protocol. Log once per session.
                    Log.Write($"PS4 output rejected: {new Win32Exception().Message}");
                    sent = current;
                }
            } while (await tick.WaitForNextTickAsync(token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { Log.Write($"PS4 output: {ex}"); }
        finally
        {
            try { HidDevices.WriteOutput(handle, Ds4Protocol.Output(device.Bluetooth, 0, 0, 0, 0, 0)); }
            catch (Exception ex) { Log.Write($"PS4 output cleanup: {ex.Message}"); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        lifetime.Cancel();
        if (task is not null) await task.ConfigureAwait(false);
        lifetime.Dispose();
    }
}

internal static class Log
{
    private static readonly object Gate = new();
    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DualController", "Logs");
    public static void Write(string message)
    {
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                string file = Path.Combine(DirectoryPath, "bridge.log");
                if (File.Exists(file) && new FileInfo(file).Length > 2_000_000)
                    File.Move(file, Path.Combine(DirectoryPath, "bridge.previous.log"), true);
                File.AppendAllText(file, $"{DateTimeOffset.UtcNow:u} {message}{Environment.NewLine}");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
