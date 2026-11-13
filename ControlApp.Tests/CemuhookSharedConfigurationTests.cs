using System.Collections.Concurrent;
using System.IO;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;
using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class CemuhookSharedConfigurationTests
{
    [Fact]
    public void FailedConfigurationSaveDoesNotReconfigureTheListener()
    {
        string root = Path.Combine(Path.GetTempPath(), "dshm-cemuhook-save-" + Guid.NewGuid().ToString("N"));
        var locations = new DshmConfigLocations(root, root);
        var data = new DshmConfigManagerUserData
            { SchemaVersion = DshmConfigManagerUserData.CurrentSchemaVersion, CemuhookServer = new() { Port = FreePort() } };
        try
        {
            using var configuration = new DshmConfigManager(data, locations);
            // A directory at the driver file path makes the save fail deterministically.
            Directory.CreateDirectory(locations.DriverConfigFilePath);
            using var service = new CemuhookBridgeService(configuration);
            service.UpdateController("owner", CemuhookControllerIdentity.Create("001122334401", "A"), true, 1, new(), () => null);
            Assert.Null(service.GetError("owner"));
            configuration.SharedCemuhookUdp.IpAddress = "nonsense";
            Assert.False(configuration.SaveChangesAndUpdateDsHidMiniConfigFile());
            Assert.Null(service.GetError("owner"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("nonsense", 26760)]
    [InlineData("127.0.0.1", 0)]
    [InlineData("127.0.0.1", 26761)]
    public void DisablingDiscardsEndpointDraftWithoutRebindingOtherControllers(string address, int port)
    {
        var endpoint = new CemuhookUdpSettings { Port = 26760 };
        var vm = new CemuhookUdpSettingsViewModel(() => CemuhookListenAddresses.CreateOptions([])) { Enabled = true };
        vm.LoadSharedEndpoint(endpoint);
        vm.IpAddress = address; vm.Port = port; vm.Enabled = false;
        Assert.True(vm.CanApply);
        Assert.True(vm.SaveSharedEndpoint(endpoint, _ => throw new InvalidOperationException("Must not rebind on disable")));
        Assert.Equal("127.0.0.1", endpoint.IpAddress);
        Assert.Equal(26760, endpoint.Port);
        Assert.Equal(endpoint.IpAddress, vm.IpAddress);
        Assert.Equal(endpoint.Port, vm.Port);
        Assert.False(vm.Enabled);
    }

    [Fact]
    public void MissingMotionDoesNotBlockUnrelatedChangesAndEnabledBridgeRemainsAccessible()
    {
        var editor = new SettingsEditorViewModel(new DeviceSettings
            { CemuhookUdp = new() { Enabled = true, IpAddress = "nonsense" } });
        var vm = editor.CemuhookUdpSettingsVM;
        Assert.False(vm.IsMotionAvailable);
        Assert.True(vm.IsGroupVisible);
        Assert.True(vm.CanApply);
        Assert.NotNull(vm.ValidationError);
        var endpoint = new CemuhookUdpSettings();
        Assert.True(vm.SaveSharedEndpoint(endpoint));
        Assert.Equal("127.0.0.1", endpoint.IpAddress);

        vm.IpAddress = "nonsense";
        editor.ApplyDeviceCapabilities(DsDeviceType.Sixaxis, DsIdentificationMotionPath.HwCal);
        Assert.False(vm.CanApply);
        vm.IsGroupVisible = false;
        Assert.True(vm.CanApply);
        Assert.True(vm.SaveSharedEndpoint(endpoint));
        vm.IsGroupVisible = true;
        vm.Enabled = false;
        Assert.True(vm.CanApply);
        editor.ApplyDeviceCapabilities(DsDeviceType.Sixaxis);
        Assert.False(vm.IsGroupVisible);
    }

    [Fact]
    public void FailedReplacementBindDoesNotPersistTheEndpointDraft()
    {
        var endpoint = new CemuhookUdpSettings { Port = FreePort() };
        using var service = new CemuhookBridgeService(() => endpoint, _ => null, (_, _) => { },
            _ => throw new InvalidOperationException("No IPC source expected"));
        service.UpdateController("owner", CemuhookControllerIdentity.Create("001122334401", "A"), true, 1, new(), () => null);
        using var occupied = new System.Net.Sockets.UdpClient();
        occupied.ExclusiveAddressUse = true;
        occupied.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        int busyPort = ((System.Net.IPEndPoint)occupied.Client.LocalEndPoint!).Port;
        var vm = new CemuhookUdpSettingsViewModel(() => CemuhookListenAddresses.CreateOptions([])) { Enabled = true };
        vm.LoadSharedEndpoint(endpoint);
        int originalPort = endpoint.Port;
        vm.Port = busyPort;
        Assert.False(vm.SaveSharedEndpoint(endpoint, service.TryUpdateEndpoint));
        Assert.Equal(originalPort, endpoint.Port);
        Assert.Equal(busyPort, vm.Port);
        Assert.Contains("already in use", vm.ErrorMessage);
        occupied.Dispose();
        Assert.True(vm.SaveSharedEndpoint(endpoint, service.TryUpdateEndpoint));
        Assert.Equal(busyPort, endpoint.Port);
    }

    [Fact]
    public void SharedEndpointRefreshPreservesPerControllerEnablementAndPendingEdits()
    {
        var vm = new CemuhookUdpSettingsViewModel(() => CemuhookListenAddresses.CreateOptions([]));
        var endpoint = new CemuhookUdpSettings { IpAddress = "127.0.0.1", Port = 26760 };
        vm.Enabled = true;
        vm.LoadSharedEndpoint(endpoint);
        Assert.True(vm.Enabled);
        endpoint.Port = 26761;
        vm.RefreshSharedEndpoint(endpoint);
        Assert.Equal(26761, vm.Port);
        vm.Port = 26762;
        endpoint.Port = 26763;
        vm.RefreshSharedEndpoint(endpoint);
        Assert.Equal(26762, vm.Port);
        vm.SaveSharedEndpoint(endpoint);
        Assert.Equal(26762, endpoint.Port);
        Assert.False(endpoint.Enabled);
        vm.LoadSharedEndpoint(endpoint);
        vm.ReportServerError(null, endpoint.IpAddress, endpoint.Port, 3);
        Assert.Equal("DSU slot 3", vm.SlotDisplay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(255)]
    public async Task ReaderRejectsSamplesThatDoNotBelongToItsCurrentIpcSlot(uint wrongSlot)
    {
        using var source = new CemuhookTestSource();
        var binding = new CemuhookIpcBinding("A", "A", 1);
        using var reader = new CemuhookMotionReader("A", new(), () => binding, _ => source, _ => { });
        var wrong = CemuhookMotionProcessorTests.Sample(1, 1); wrong.SlotIndex = wrongSlot;
        source.Enqueue(wrong);
        await CemuhookTestSource.Eventually(() => source.Reads >= 2);
        Assert.False(reader.LatestSnapshot.Connected);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(2, 1.005));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected);
    }

    [Fact]
    public void FifthControllerDoesNotStealAnActiveSlotAndCanUseAFreedSlot()
    {
        var endpoint = new CemuhookUdpSettings { Port = FreePort() };
        var sources = new ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource>();
        using var service = new CemuhookBridgeService(() => endpoint, _ => null, (_, _) => { }, binding => sources[binding]);
        object[] owners = Enumerable.Range(0, 5).Select(_ => new object()).ToArray();
        for (int i = 0; i < 5; i++)
        {
            var identity = CemuhookControllerIdentity.Create($"0011223344{i:X2}", $"PAD-{i}");
            var binding = new CemuhookIpcBinding(identity.Key, $"PAD-{i}", i + 1);
            sources[binding] = new();
            service.UpdateController(owners[i], identity, true, 1, new(), () => binding);
        }
        Assert.Null(service.GetSlot(owners[4]));
        Assert.Contains("four", service.GetError(owners[4]));
        for (int i = 0; i < 4; i++) Assert.Equal(i, service.GetSlot(owners[i]));
        service.RemoveController(owners[0]);
        var fifth = CemuhookControllerIdentity.Create("001122334404", "PAD-4");
        service.UpdateController(owners[4], fifth, true, 1, new(), () => new(fifth.Key, "PAD-4", 5));
        Assert.Equal(0, service.GetSlot(owners[4]));
        Assert.Null(service.GetError(owners[4]));
        for (int i = 1; i < 4; i++) Assert.Equal(i, service.GetSlot(owners[i]));
    }

    [Fact]
    public void PreferredSlotsSurviveSharedServiceRecreationAndDifferentConnectionOrder()
    {
        var endpoint = new CemuhookUdpSettings { Port = FreePort() };
        var preferences = new Dictionary<string, int>();
        var a = CemuhookControllerIdentity.Create("001122334401", "A");
        var b = CemuhookControllerIdentity.Create("001122334402", "B");
        var sources = new ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource>();
        CemuhookBridgeService Create() => new(() => endpoint,
            key => preferences.TryGetValue(key, out int slot) ? slot : null,
            (key, slot) => preferences[key] = slot,
            binding => sources.GetOrAdd(binding, _ => new()));
        using (var first = Create())
        {
            first.UpdateController("owner-a", a, true, 1, new(), () => null);
            first.UpdateController("owner-b", b, true, 1, new(), () => null);
            Assert.Equal(0, first.GetSlot("owner-a")); Assert.Equal(1, first.GetSlot("owner-b"));
        }
        using var second = Create();
        second.UpdateController("owner-b", b, true, 1, new(), () => null);
        second.UpdateController("owner-a", a, true, 1, new(), () => null);
        Assert.Equal(0, second.GetSlot("owner-a")); Assert.Equal(1, second.GetSlot("owner-b"));
    }

    private static int FreePort()
    {
        using var socket = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        return ((System.Net.IPEndPoint)socket.Client.LocalEndPoint!).Port;
    }
}
