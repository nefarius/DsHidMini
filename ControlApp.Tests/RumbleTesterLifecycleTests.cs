using Nefarius.DsHidMini.ControlApp.Models.Rumble;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Wpf.Ui.Controls;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class RumbleTesterLifecycleTests
{
    [Fact]
    public void NewSession_StartsReadyAtMidStrength()
    {
        var output = new ScriptedRumbleOutput();
        RumbleTesterViewModel vm = new(1, "AA:BB:CC:DD:EE:FF", () => output, TimeSpan.Zero);

        Assert.Equal("Rumble tester — AA:BB:CC:DD:EE:FF", vm.Title);
        Assert.Equal(RumbleTesterStatus.Ready, vm.StatusText);
        Assert.Equal(128, vm.LargeMotor);
        Assert.Equal(128, vm.SmallMotor);
        Assert.False(vm.IsPulsing);
        Assert.False(vm.IsShutdown);

        vm.Dispose();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(0.4, 0)]
    [InlineData(0.5, 1)]
    [InlineData(200.4, 200)]
    [InlineData(200.5, 201)]
    [InlineData(255, 255)]
    [InlineData(300, 255)]
    public void Slider_RoundsToByteStrength(double value, byte expected)
    {
        Assert.Equal(expected, RumbleStrength.FromSlider(value));
    }

    [Fact]
    public void Slider_RejectsNonFiniteValues()
    {
        Assert.Equal((byte)0, RumbleStrength.FromSlider(double.NaN));
        Assert.Equal((byte)0, RumbleStrength.FromSlider(double.PositiveInfinity));
        Assert.Equal((byte)0, RumbleStrength.FromSlider(double.NegativeInfinity));
    }

    [Fact]
    public async Task Pulse_SendsRoundedStrengthThenTurnsOff()
    {
        var output = new ScriptedRumbleOutput();
        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.Zero)
        {
            LargeMotor = 200.4,
            SmallMotor = 10.6
        };

        await vm.TestBothCommand.ExecuteAsync(null);

        Assert.Equal(new[] { ((byte)200, (byte)11), ((byte)0, (byte)0) }, output.Calls);
        Assert.Equal(RumbleTesterStatus.Off, vm.StatusText);
        Assert.Equal(InfoBarSeverity.Informational, vm.StatusSeverity);
        Assert.False(vm.IsPulsing);
    }

    [Fact(Timeout = 5000)]
    public async Task Replacement_CancelsPreviousPulseBeforeItTurnsOff()
    {
        var output = new ScriptedRumbleOutput();
        var firstOn = NewSignal();
        var secondOn = NewSignal();
        output.Handler = (large, small) =>
        {
            if (large == 30 && small == 0)
            {
                firstOn.TrySetResult();
            }
            else if (large == 0 && small == 40)
            {
                secondOn.TrySetResult();
            }

            return 0;
        };

        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.FromSeconds(30))
        {
            LargeMotor = 30,
            SmallMotor = 40
        };

        Task first = vm.TestLargeCommand.ExecuteAsync(null);
        await firstOn.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task second = vm.TestSmallCommand.ExecuteAsync(null);
        await secondOn.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(new[] { ((byte)30, (byte)0), ((byte)0, (byte)40) }, output.Calls);

        await vm.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(2));
        await first.WaitAsync(TimeSpan.FromSeconds(2));
        await second.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(new[] { ((byte)30, (byte)0), ((byte)0, (byte)40), ((byte)0, (byte)0) }, output.Calls);
        Assert.True(output.DisposedAfterOff);
    }

    [Fact(Timeout = 5000)]
    public async Task Pulse_ShowsStallThenClearsWhenOutputRecovers()
    {
        var stalled = new int[1];
        var output = new ScriptedRumbleOutput();
        TaskCompletionSource on = NewSignal();
        output.Handler = (large, small) =>
        {
            if (large != 0 || small != 0)
            {
                Interlocked.Exchange(ref stalled[0], 1);
                on.TrySetResult();
            }

            return 0;
        };

        const string note = "Pair a controller to the receiver.";
        RumbleTesterViewModel vm = new(
            1,
            "test",
            () => output,
            TimeSpan.FromSeconds(30),
            () => Volatile.Read(ref stalled[0]) != 0,
            note,
            TimeSpan.FromMilliseconds(20))
        {
            LargeMotor = 25
        };

        Task pulse = vm.TestLargeCommand.ExecuteAsync(null);
        await on.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await WaitUntil(() =>
            vm.IsOutputStalled && vm.StatusText == RumbleTesterStatus.Pulsing(25, 0));
        Assert.Equal(InfoBarSeverity.Informational, vm.StatusSeverity);

        Interlocked.Exchange(ref stalled[0], 0);
        await WaitUntil(() => !vm.IsOutputStalled);

        await vm.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(2));
        await pulse.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact(Timeout = 5000)]
    public async Task StalePoll_DoesNotClearStallObservedAfterSend()
    {
        var releasePoll = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource pollEntered = NewSignal();
        int reads = 0;
        var output = new ScriptedRumbleOutput();
        RumbleTesterViewModel vm = new(
            1,
            "test",
            () => output,
            TimeSpan.FromSeconds(30),
            () =>
            {
                if (Interlocked.Increment(ref reads) == 1)
                {
                    pollEntered.TrySetResult();
                    return releasePoll.Task.GetAwaiter().GetResult();
                }

                return true;
            },
            "Pair a controller to the receiver.",
            TimeSpan.FromSeconds(30))
        {
            LargeMotor = 25
        };

        await pollEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task pulse = vm.TestLargeCommand.ExecuteAsync(null);
        await WaitUntil(() =>
            vm.IsOutputStalled && vm.StatusText == RumbleTesterStatus.Pulsing(25, 0));

        releasePoll.TrySetResult(false);
        DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(200);
        while (DateTimeOffset.UtcNow < deadline)
        {
            Assert.True(vm.IsOutputStalled);
            Assert.Equal(RumbleTesterStatus.Pulsing(25, 0), vm.StatusText);
            await Task.Delay(10);
        }

        await vm.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(2));
        await pulse.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RejectedPulse_WhileStalled_ShowsStallGuidance()
    {
        const uint rejected = 0xC0000001;
        bool stalled = false;
        var output = new ScriptedRumbleOutput
        {
            Handler = (large, small) =>
            {
                if (large == 0 && small == 0)
                {
                    return 0;
                }

                stalled = true;
                return rejected;
            }
        };

        const string note = "Pair a controller to the receiver.";
        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.Zero, () => stalled, note)
        {
            LargeMotor = 40
        };

        await vm.TestLargeCommand.ExecuteAsync(null);

        Assert.True(vm.IsOutputStalled);
        Assert.Equal(note, vm.StatusText);
        Assert.Equal(InfoBarSeverity.Warning, vm.StatusSeverity);
        Assert.Equal(new[] { ((byte)40, (byte)0), ((byte)0, (byte)0) }, output.Calls);

        vm.Dispose();
    }

    [Fact]
    public async Task RejectedPulse_ReportsNtStatusAndStillTurnsOff()
    {
        const uint rejected = 0xC000000D;
        var output = new ScriptedRumbleOutput
        {
            Handler = (large, small) => large == 0 && small == 0 ? 0 : rejected
        };
        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.Zero)
        {
            LargeMotor = 40
        };

        await vm.TestLargeCommand.ExecuteAsync(null);

        Assert.Equal(RumbleTesterStatus.Rejected(rejected), vm.StatusText);
        Assert.Equal(InfoBarSeverity.Error, vm.StatusSeverity);
        Assert.Equal(new[] { ((byte)40, (byte)0), ((byte)0, (byte)0) }, output.Calls);
    }

    [Fact(Timeout = 5000)]
    public async Task Stop_TurnsOffBeforeThePulseDurationElapses()
    {
        var output = new ScriptedRumbleOutput();
        TaskCompletionSource on = NewSignal();
        output.Handler = (large, small) =>
        {
            if (large != 0 || small != 0)
            {
                on.TrySetResult();
            }

            return 0;
        };
        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.FromSeconds(30))
        {
            LargeMotor = 25
        };

        Task pulse = vm.TestLargeCommand.ExecuteAsync(null);
        await on.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await vm.StopCommand.ExecuteAsync(null);
        await pulse.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(new[] { ((byte)25, (byte)0), ((byte)0, (byte)0) }, output.Calls);
        Assert.Equal(RumbleTesterStatus.Off, vm.StatusText);
        Assert.False(vm.IsPulsing);
    }

    [Fact]
    public async Task Shutdown_IsIdempotentAndAcknowledgesOneOffCommand()
    {
        var output = new ScriptedRumbleOutput();
        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.Zero);

        Task<RumbleCommandResult> first = vm.ShutdownAsync();
        Task<RumbleCommandResult> second = vm.ShutdownAsync();

        Assert.Same(first, second);
        RumbleCommandResult result = await first.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(result.Succeeded);
        Assert.Equal(new[] { ((byte)0, (byte)0) }, output.Calls);
        Assert.Equal(1, output.DisposeCount);
        Assert.True(output.DisposedAfterOff);
        Assert.True(vm.IsShutdown);

        vm.Dispose();
        Assert.Single(output.Calls);
        Assert.Equal(1, output.DisposeCount);
    }

    [Fact]
    public async Task Shutdown_CompletesWhenOffCommandThrows()
    {
        var output = new ScriptedRumbleOutput
        {
            Handler = (_, _) => throw new InvalidOperationException("timed out")
        };
        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.Zero);

        RumbleCommandResult result = await vm.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(result.Succeeded);
        Assert.Equal("timed out", result.Error);
        Assert.True(vm.IsShutdown);
        Assert.True(output.IsDisposed);
        Assert.Single(output.Calls);
        Assert.Equal(((byte)0, (byte)0), output.Calls[0]);
    }

    [Fact(Timeout = 5000)]
    public async Task Shutdown_WaitsForInFlightCommandThenAcknowledgedOff()
    {
        var output = new ScriptedRumbleOutput();
        TaskCompletionSource onEntered = NewSignal();
        var releaseOn = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource offEntered = NewSignal();
        var releaseOff = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        output.Handler = (large, small) =>
        {
            if (large == 0 && small == 0)
            {
                offEntered.TrySetResult();
                return releaseOff.Task.GetAwaiter().GetResult();
            }

            onEntered.TrySetResult();
            return releaseOn.Task.GetAwaiter().GetResult();
        };

        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.FromSeconds(30))
        {
            LargeMotor = 30
        };

        Task pulse = vm.TestLargeCommand.ExecuteAsync(null);
        await onEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task shutdown = vm.ShutdownAsync();

        await Task.Delay(50);
        Assert.False(shutdown.IsCompleted);
        Assert.DoesNotContain(output.Calls, call => call == ((byte)0, (byte)0));

        releaseOn.TrySetResult(0);
        await offEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(shutdown.IsCompleted);
        Assert.False(output.IsDisposed);

        releaseOff.TrySetResult(0);
        await shutdown.WaitAsync(TimeSpan.FromSeconds(2));
        await pulse.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(output.DisposedAfterOff);
        Assert.Equal(((byte)0, (byte)0), output.Calls[^1]);
    }

    [Fact(Timeout = 5000)]
    public async Task Dispose_DoesNotCompleteBeforeOffIsAcknowledged()
    {
        var output = new ScriptedRumbleOutput();
        TaskCompletionSource offEntered = NewSignal();
        var releaseOff = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        output.Handler = (_, _) =>
        {
            offEntered.TrySetResult();
            return releaseOff.Task.GetAwaiter().GetResult();
        };
        RumbleTesterViewModel vm = new(1, "test", () => output, TimeSpan.Zero);

        Task dispose = Task.Run(vm.Dispose);
        await offEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(dispose.IsCompleted);
        Assert.False(output.IsDisposed);

        releaseOff.TrySetResult(0);
        await dispose.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(output.DisposedAfterOff);
        Assert.Equal(1, output.DisposeCount);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("Condition was not met.");
            }

            await Task.Delay(10);
        }
    }

    private static TaskCompletionSource NewSignal()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ScriptedRumbleOutput : IRumbleOutput
    {
        public List<(byte Large, byte Small)> Calls { get; } = new();

        public Func<byte, byte, uint>? Handler { get; set; }

        public bool IsDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public bool DisposedAfterOff { get; private set; }

        public uint SetRumble(byte largeMotor, byte smallMotor)
        {
            Calls.Add((largeMotor, smallMotor));
            return Handler?.Invoke(largeMotor, smallMotor) ?? 0;
        }

        public void Dispose()
        {
            DisposeCount++;
            DisposedAfterOff = Calls.Count > 0 && Calls[^1] == (0, 0);
            IsDisposed = true;
        }
    }
}
