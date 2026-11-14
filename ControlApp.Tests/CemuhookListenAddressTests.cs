using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class CemuhookListenAddressTests
{
    [Fact]
    public async Task BoundComboBoxKeepsSelectionAcrossEditsAndNetworkRefreshes()
    {
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                IReadOnlyList<CemuhookListenAddress> available = AdapterChoices();
                var vm = new CemuhookUdpSettingsViewModel(() => available) { Enabled = true };
                var combo = new ComboBox { DataContext = vm, IsEditable = false, DisplayMemberPath = "DisplayName" };
                combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(vm.ListenAddresses)));
                combo.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(vm.SelectedListenAddress)) { Mode = BindingMode.TwoWay });
                void CheckSelection(string address, bool isAvailable)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    var selected = Assert.IsType<CemuhookListenAddress>(combo.SelectedItem);
                    Assert.Equal(address, selected.Address);
                    Assert.Equal(isAvailable, selected.IsAvailable);
                    Assert.Equal(address, vm.IpAddress);
                }

                CheckSelection("127.0.0.1", true);
                vm.IpAddress = "192.0.2.20";
                CheckSelection("192.0.2.20", false);
                vm.IpAddress = "127.0.0.1";
                CheckSelection("127.0.0.1", true);
                vm.IpAddress = "192.168.1.42";
                CheckSelection("192.168.1.42", true);
                available = CemuhookListenAddresses.CreateOptions([]);
                vm.RefreshListenAddresses();
                CheckSelection("192.168.1.42", false);
                available = AdapterChoices();
                vm.RefreshListenAddresses();
                CheckSelection("192.168.1.42", true);
                finished.TrySetResult();
            }
            catch (Exception error)
            {
                finished.TrySetException(error);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void OptionsIncludeDefaultsAndEachDistinctLocalIpv4AddressWithItsAdapterName()
    {
        var choices = CemuhookListenAddresses.CreateOptions([
            ("Wi-Fi", IPAddress.Parse("192.168.1.75")),
            ("Ethernet", IPAddress.Parse("192.168.1.42")),
            ("Ethernet", IPAddress.Parse("192.168.1.43")),
            ("Duplicate", IPAddress.Parse("192.168.1.42")),
            ("Loopback", IPAddress.Loopback),
            ("IPv6", IPAddress.IPv6Loopback)]);
        Assert.Equal("127.0.0.1", choices[0].Address);
        Assert.Equal("0.0.0.0", choices[1].Address);
        Assert.Equal(5, choices.Count);
        Assert.Contains(choices, choice => choice.DisplayName == "Wi-Fi — 192.168.1.75");
        Assert.Contains(choices, choice => choice.DisplayName == "Ethernet — 192.168.1.43");
        Assert.All(choices, choice => Assert.True(choice.IsAvailable));
    }

    [Fact]
    public void DefaultIsLocalOnlyAndSelectingAnAdapterPersistsItsAddress()
    {
        var vm = CreateSettings();
        Assert.Equal("127.0.0.1", vm.SelectedListenAddress!.Address);
        vm.SelectedListenAddress = vm.ListenAddresses.Single(choice => choice.Address == "192.168.1.42");
        vm.Enabled = true;
        var destination = new DeviceSettings();
        vm.SaveSettingsToBackingDataContainer(destination);
        Assert.Equal("192.168.1.42", destination.CemuhookUdp.IpAddress);
        Assert.True(destination.CemuhookUdp.Enabled);
        Assert.True(vm.CanApply);
        Assert.False(vm.HasError);
    }

    [Theory]
    [InlineData("192.0.2.20")]
    [InlineData("nonsense")]
    [InlineData("")]
    [InlineData("::1")]
    public void UnavailableSavedAddressIsPreservedAndBlocksApplyWhenEnabled(string address)
    {
        var vm = CreateSettings();
        var saved = new DeviceSettings { CemuhookUdp = new() { Enabled = true, IpAddress = address } };
        vm.LoadSettingsFromBackingDataContainer(saved);
        Assert.Equal(address, vm.IpAddress);
        Assert.NotNull(vm.SelectedListenAddress);
        Assert.False(vm.SelectedListenAddress.IsAvailable);
        Assert.Contains("unavailable", vm.SelectedListenAddress.DisplayName);
        Assert.False(vm.CanApply);
        Assert.True(vm.HasError);
        Assert.False(string.IsNullOrWhiteSpace(vm.ErrorMessage));
        Assert.Equal(address, saved.CemuhookUdp.IpAddress);
        // Disabling an invalid saved server must always be possible.
        vm.Enabled = false;
        Assert.True(vm.CanApply);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void RefreshTracksAdapterDisappearanceAndReturnWithoutChangingSavedSelection()
    {
        IReadOnlyList<CemuhookListenAddress> available = AdapterChoices();
        var vm = new CemuhookUdpSettingsViewModel(() => available) { Enabled = true, IpAddress = "192.168.1.42" };
        Assert.True(vm.CanApply);
        available = CemuhookListenAddresses.CreateOptions([]);
        vm.RefreshListenAddresses();
        // WPF can clear SelectedItem while ItemsSource is replaced.
        vm.SelectedListenAddress = null;
        Assert.Equal("192.168.1.42", vm.IpAddress);
        Assert.False(vm.SelectedListenAddress!.IsAvailable);
        Assert.False(vm.CanApply);
        available = AdapterChoices();
        vm.RefreshListenAddresses();
        Assert.True(vm.SelectedListenAddress!.IsAvailable);
        Assert.True(vm.CanApply);
        Assert.False(vm.HasError);
        Assert.Single(vm.ListenAddresses, choice => choice.Address == "192.168.1.42");
    }

    [Fact]
    public void ChoosingLocalOnlyOrResettingResolvesAnUnavailableSavedAddress()
    {
        var vm = CreateSettings();
        vm.Enabled = true;
        vm.IpAddress = "nonsense";
        vm.SelectedListenAddress = vm.ListenAddresses.First();
        Assert.True(vm.CanApply);
        Assert.Equal("127.0.0.1", vm.IpAddress);
        vm.IpAddress = "192.0.2.20";
        vm.ResetGroupToOriginalDefaults();
        Assert.False(vm.Enabled);
        Assert.Equal("127.0.0.1", vm.SelectedListenAddress!.Address);
        Assert.True(vm.CanApply);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void InvalidSavedPortHasVisibleValidationAndIsNotSilentlyClamped(int port)
    {
        var vm = CreateSettings();
        vm.LoadSettingsFromBackingDataContainer(new DeviceSettings { CemuhookUdp = new() { Enabled = true, Port = port } });
        Assert.Equal(port, vm.Port);
        Assert.False(vm.CanApply);
        Assert.Contains("between 1 and 65535", vm.ErrorMessage);
        vm.Port = 26760;
        Assert.True(vm.CanApply);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void RuntimeErrorDoesNotBlockRetryAndDoesNotFollowEditsToAnotherEndpoint()
    {
        var vm = CreateSettings();
        vm.Enabled = true;
        vm.ReportServerError("port occupied", "127.0.0.1", 26760);
        Assert.True(vm.HasError);
        Assert.Equal("port occupied", vm.ErrorMessage);
        Assert.True(vm.CanApply);
        vm.Port = 26761;
        Assert.False(vm.HasError);
        vm.ReportServerError("old endpoint still occupied", "127.0.0.1", 26760);
        Assert.False(vm.HasError);
        vm.ReportServerError("new port occupied", "127.0.0.1", 26761);
        Assert.True(vm.HasError);
        vm.SelectedListenAddress = vm.ListenAddresses.Single(choice => choice.Address == "0.0.0.0");
        Assert.False(vm.HasError);
        vm.ReportServerError("old endpoint still occupied", "127.0.0.1", 26761);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void EnumerationFailureKeepsDefaultsAndShowsARecoverableMessage()
    {
        bool fail = true;
        var vm = new CemuhookUdpSettingsViewModel(() => fail ? throw new NetworkInformationException() : AdapterChoices());
        vm.Enabled = true;
        Assert.Equal("127.0.0.1", vm.SelectedListenAddress!.Address);
        Assert.True(vm.CanApply);
        Assert.Contains("refresh network addresses", vm.ErrorMessage);
        fail = false;
        vm.RefreshListenAddresses();
        Assert.False(vm.HasError);
        Assert.Contains(vm.ListenAddresses, choice => choice.Address == "192.168.1.42");
    }

    [Fact]
    public void OccupiedPortIsVisibleAndCanBeRetriedAfterThePortIsReleased()
    {
        using var occupied = new UdpClient();
        occupied.ExclusiveAddressUse = true;
        occupied.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)occupied.Client.LocalEndPoint!).Port;
        var udp = new CemuhookUdpSettings { Enabled = true, Port = port };
        var vm = CreateSettings();
        vm.LoadSettingsFromBackingDataContainer(new DeviceSettings { CemuhookUdp = udp });
        using var host = new CemuhookBridgeHost(settings => new CemuhookServer(settings.Port, settings.IpAddress, CemuhookPadState.Empty));
        host.StatusChanged += (_, _) => vm.ReportServerError(host.ErrorMessage, udp.IpAddress, udp.Port);
        host.Update(udp, udp.Enabled);
        Assert.Contains("already in use", host.ErrorMessage);
        Assert.Contains(port.ToString(), host.ErrorMessage);
        Assert.Equal(host.ErrorMessage, vm.ErrorMessage);
        Assert.True(vm.HasError);
        Assert.True(vm.CanApply);
        occupied.Dispose();
        host.Update(udp, udp.Enabled);
        Assert.Null(host.ErrorMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void DisappearingInterfaceBindFailureIsVisibleWithoutFallingBackToAllInterfaces()
    {
        string? attempted = null;
        var udp = new CemuhookUdpSettings { Enabled = true, IpAddress = "192.168.1.42" };
        using var host = new CemuhookBridgeHost(settings =>
        {
            attempted = settings.IpAddress;
            throw new SocketException((int)SocketError.AddressNotAvailable);
        });
        host.Update(udp, udp.Enabled);
        Assert.Equal("192.168.1.42", attempted);
        Assert.Equal(attempted, udp.IpAddress);
        Assert.Contains("no longer available", host.ErrorMessage);
    }

    [Theory]
    [InlineData("nonsense", 26760)]
    [InlineData("127.0.0.1", 0)]
    [InlineData("127.0.0.1", 65536)]
    public void HostRejectsInvalidSavedEndpointsBeforeStartingAWorker(string address, int port)
    {
        bool started = false;
        using var host = new CemuhookBridgeHost(_ => { started = true; throw new InvalidOperationException("Unexpected start"); });
        var udp = new CemuhookUdpSettings { Enabled = true, IpAddress = address, Port = port };
        host.Update(udp, udp.Enabled);
        Assert.False(started);
        Assert.False(string.IsNullOrWhiteSpace(host.ErrorMessage));
        udp.Enabled = false;
        host.Update(udp, udp.Enabled);
        Assert.Null(host.ErrorMessage);
    }

    [Fact]
    public async Task WorkerStartupFailureRaisesStatusWithoutWaitingForTheDeviceRefreshTimer()
    {
        var failure = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new CemuhookBridgeHost(settings => new CemuhookServer(settings.Port, settings.IpAddress,
            _ => throw new InvalidOperationException("Motion telemetry is unavailable.")));
        host.StatusChanged += (_, _) =>
        {
            if (host.ErrorMessage is { } error)
                failure.TrySetResult(error);
        };
        host.Update(new() { Enabled = true, Port = FreePort() }, true);
        string message = await failure.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("Motion telemetry is unavailable", message);
        host.Dispose();
        Assert.Null(host.ErrorMessage);
    }

    private static CemuhookUdpSettingsViewModel CreateSettings() => new(AdapterChoices);
    [Fact]
    public async Task FailedEndpointReplacementKeepsTheWorkingListener()
    {
        using var occupied = new UdpClient();
        occupied.ExclusiveAddressUse = true;
        occupied.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        int busyPort = ((IPEndPoint)occupied.Client.LocalEndPoint!).Port;
        var endpoint = new CemuhookUdpSettings { Port = FreePort() };
        using var host = new CemuhookBridgeHost(settings => new(settings.Port, settings.IpAddress, CemuhookPadState.Empty));
        Assert.True(host.Update(endpoint, true));
        byte[] before = await VersionReply(endpoint.Port);
        Assert.False(host.Update(new() { Port = busyPort }, true));
        Assert.Contains("already in use", host.ErrorMessage);
        Assert.Equal(before, await VersionReply(endpoint.Port));
        Assert.False(host.Update(new() { IpAddress = "nonsense", Port = endpoint.Port }, true));
        Assert.Equal(before, await VersionReply(endpoint.Port));
    }

    [Fact]
    public async Task FailedAddressChangeOnTheSamePortRestoresThePreviousBinding()
    {
        var endpoint = new CemuhookUdpSettings { Port = FreePort() };
        using var host = new CemuhookBridgeHost(settings => settings.IpAddress == "0.0.0.0"
            ? throw new SocketException((int)SocketError.AccessDenied)
            : new(settings.Port, settings.IpAddress, CemuhookPadState.Empty));
        Assert.True(host.Update(endpoint, true));
        Assert.False(host.Update(new() { IpAddress = "0.0.0.0", Port = endpoint.Port }, true));
        Assert.Contains("denied", host.ErrorMessage);
        Assert.NotEmpty(await VersionReply(endpoint.Port));
        Assert.True(host.Update(endpoint, true));
        Assert.Null(host.ErrorMessage);
    }

    [Fact]
    public async Task ConcurrentEndpointUpdatesAndDisposalSerializeSocketOwnership()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var endpoint = new CemuhookUdpSettings { Port = FreePort() };
        int starts = 0;
        using var host = new CemuhookBridgeHost(settings =>
        {
            Interlocked.Increment(ref starts);
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
            return new(settings.Port, settings.IpAddress, CemuhookPadState.Empty);
        });
        Task first = Task.Run(() => host.Update(endpoint, true));
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        using var secondEntered = new ManualResetEventSlim();
        Task second = Task.Run(() =>
        {
            secondEntered.Set();
            for (int i = 0; i < 20; i++) host.Update(endpoint, i % 2 == 0);
        });
        Task dispose = Task.Run(host.Dispose);
        try
        {
            Assert.True(secondEntered.Wait(TimeSpan.FromSeconds(5)));
            Assert.False(second.IsCompleted);
            Assert.False(dispose.IsCompleted);
            Assert.Equal(1, Volatile.Read(ref starts));
        }
        finally { release.Set(); }
        await Task.WhenAll(first, second, dispose).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(host.Update(endpoint, true));
        using var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, endpoint.Port));
    }

    private static async Task<byte[]> VersionReply(int port)
    {
        using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        await client.SendAsync(CemuhookServerLifecycleTests.ClientPacket(DsuPacket.MessageVersion, []),
            new IPEndPoint(IPAddress.Loopback, port));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return (await client.ReceiveAsync(timeout.Token)).Buffer;
    }
    private static IReadOnlyList<CemuhookListenAddress> AdapterChoices() =>
        CemuhookListenAddresses.CreateOptions([("Ethernet", IPAddress.Parse("192.168.1.42"))]);

    private static int FreePort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }

}
