using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.Services;

using Wpf.Ui.Controls;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DshmIpcStatusServiceTests
{
    [Fact]
    public void Refresh_MissingValue_IsDisabledAndKnown()
    {
        FakeStore store = new() { Value = null };
        DshmIpcStatusService service = new(store, static () => true);

        service.Refresh();

        Assert.True(service.IsStateKnown);
        Assert.False(service.IsEnabled);
        Assert.Equal("Disabled", service.StateDisplay);
        Assert.True(service.CanEnable);
        Assert.False(service.CanDisable);
        Assert.Equal(InfoBarSeverity.Informational, service.Severity);
    }

    [Fact]
    public void Refresh_ZeroValue_IsDisabled()
    {
        FakeStore store = new() { Value = 0 };
        DshmIpcStatusService service = new(store, static () => false);

        service.Refresh();

        Assert.True(service.IsStateKnown);
        Assert.False(service.IsEnabled);
        Assert.Equal("Disabled", service.StateDisplay);
        Assert.False(service.CanEnable);
        Assert.False(service.CanDisable);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(-1)]
    public void Refresh_NonzeroValue_IsEnabled(int stored)
    {
        FakeStore store = new() { Value = stored };
        DshmIpcStatusService service = new(store, static () => true);

        service.Refresh();

        Assert.True(service.IsStateKnown);
        Assert.True(service.IsEnabled);
        Assert.Equal("Enabled", service.StateDisplay);
        Assert.False(service.CanEnable);
        Assert.True(service.CanDisable);
        Assert.Equal(InfoBarSeverity.Warning, service.Severity);
    }

    [Fact]
    public void Refresh_ReadFailure_IsUnknownAndCommandsDisabled()
    {
        FakeStore store = new() { ReadSucceeds = false };
        DshmIpcStatusService service = new(store, static () => true);

        service.Refresh();

        Assert.False(service.IsStateKnown);
        Assert.False(service.IsEnabled);
        Assert.Equal("Unknown", service.StateDisplay);
        Assert.False(service.CanEnable);
        Assert.False(service.CanDisable);
        Assert.Equal(InfoBarSeverity.Warning, service.Severity);
    }

    [Fact]
    public void TrySetEnabled_NonElevated_DoesNotWrite()
    {
        FakeStore store = new() { Value = 0 };
        DshmIpcStatusService service = new(store, static () => false);

        bool changed = service.TrySetEnabled(true);

        Assert.False(changed);
        Assert.False(store.WriteCalled);
        Assert.Equal(0, store.Value);
        Assert.False(service.CanEnable);
        Assert.False(service.CanDisable);
    }

    [Fact]
    public void TrySetEnabled_ElevatedEnable_WritesDwordOneAndRefreshesCommands()
    {
        FakeStore store = new() { Value = 0 };
        DshmIpcStatusService service = new(store, static () => true);

        bool changed = service.TrySetEnabled(true);

        Assert.True(changed);
        Assert.True(store.WriteCalled);
        Assert.Equal(1, store.LastWritten);
        Assert.Equal(1, store.Value);
        Assert.True(service.IsEnabled);
        Assert.False(service.CanEnable);
        Assert.True(service.CanDisable);
    }

    [Fact]
    public void TrySetEnabled_ElevatedDisable_WritesDwordZeroAndRefreshesCommands()
    {
        FakeStore store = new() { Value = 1 };
        DshmIpcStatusService service = new(store, static () => true);

        bool changed = service.TrySetEnabled(false);

        Assert.True(changed);
        Assert.True(store.WriteCalled);
        Assert.Equal(0, store.LastWritten);
        Assert.Equal(0, store.Value);
        Assert.False(service.IsEnabled);
        Assert.True(service.CanEnable);
        Assert.False(service.CanDisable);
    }

    [Fact]
    public void TrySetEnabled_WriteFailure_ReturnsFalseAndRefreshes()
    {
        FakeStore store = new()
        {
            Value = 0,
            WriteException = new UnauthorizedAccessException("denied")
        };
        DshmIpcStatusService service = new(store, static () => true);

        bool changed = service.TrySetEnabled(true);

        Assert.False(changed);
        Assert.True(store.WriteCalled);
        Assert.False(service.IsEnabled);
        Assert.True(service.CanEnable);
        Assert.False(service.CanDisable);
    }

    private sealed class FakeStore : IDshmDriverParametersStore
    {
        public int? LastWritten { get; private set; }

        public bool ReadSucceeds { get; init; } = true;

        public int? Value { get; set; }

        public Exception? WriteException { get; init; }

        public bool WriteCalled { get; private set; }

        public bool TryReadIpcEnabled(out int? value)
        {
            value = Value;
            return ReadSucceeds;
        }

        public void WriteIpcEnabled(int value)
        {
            WriteCalled = true;
            LastWritten = value;
            if (WriteException is not null)
            {
                throw WriteException;
            }

            Value = value;
        }
    }
}
