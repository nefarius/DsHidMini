using System.Windows;

using Nefarius.DsHidMini.ControlApp.Models;

using Newtonsoft.Json;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class ApplicationConfigurationAndTrayPolicyTests
{
    [Fact]
    public void ApplicationConfiguration_JsonRoundTrip_PreservesMinimizeToTray()
    {
        ApplicationConfiguration original = new()
        {
            MinimizeToTray = true,
            IsLoggingEnabled = true,
            IsUpdateCheckEnabled = false
        };

        string json = JsonConvert.SerializeObject(original);
        ApplicationConfiguration? loaded = JsonConvert.DeserializeObject<ApplicationConfiguration>(json);

        Assert.NotNull(loaded);
        Assert.True(loaded.MinimizeToTray);
        Assert.True(loaded.IsLoggingEnabled);
        Assert.False(loaded.IsUpdateCheckEnabled);
        Assert.Null(loaded.LastUpdateCheckDate);
    }

    [Fact]
    public void ApplicationConfiguration_Default_MinimizeToTrayIsOff()
    {
        ApplicationConfiguration config = new();

        Assert.False(config.MinimizeToTray);
        Assert.True(config.IsUpdateCheckEnabled);
        Assert.Null(config.LastUpdateCheckDate);
    }

    [Fact]
    public void ApplicationConfiguration_JsonRoundTrip_PreservesLastUpdateCheckDate()
    {
        DateOnly checkedOn = new(2026, 9, 8);
        ApplicationConfiguration original = new()
        {
            IsUpdateCheckEnabled = true,
            LastUpdateCheckDate = checkedOn
        };

        string json = JsonConvert.SerializeObject(original);
        ApplicationConfiguration? loaded = JsonConvert.DeserializeObject<ApplicationConfiguration>(json);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsUpdateCheckEnabled);
        Assert.Equal(checkedOn, loaded.LastUpdateCheckDate);
    }

    [Fact]
    public void ApplicationConfiguration_JsonRoundTrip_PreservesWindowPlacement()
    {
        ApplicationConfiguration original = new()
        {
            WindowLeft = -1100,
            WindowTop = 100,
            WindowWidth = 1280,
            WindowHeight = 800,
            WindowState = WindowState.Maximized
        };

        string json = JsonConvert.SerializeObject(original);
        ApplicationConfiguration? loaded = JsonConvert.DeserializeObject<ApplicationConfiguration>(json);

        Assert.NotNull(loaded);
        Assert.Equal(-1100, loaded.WindowLeft);
        Assert.Equal(100, loaded.WindowTop);
        Assert.Equal(1280, loaded.WindowWidth);
        Assert.Equal(800, loaded.WindowHeight);
        Assert.Equal(WindowState.Maximized, loaded.WindowState);
    }

    [Fact]
    public void WindowPlacementPolicy_TryRead_RejectsMissingSize()
    {
        Assert.False(WindowPlacementPolicy.TryRead(new ApplicationConfiguration(), out _));
    }

    [Fact]
    public void WindowPlacementPolicy_Resolve_KeepsPlacementWhenTitleBarIsOnAMonitor()
    {
        WindowPlacementSnapshot saved = new(1920, 80, 1200, 700, WindowState.Normal, true);
        Rect primary = new(0, 0, 1920, 1080);
        Rect secondary = new(1920, 0, 1920, 1080);

        WindowPlacementSnapshot resolved = WindowPlacementPolicy.Resolve(saved, [primary, secondary], primary);

        Assert.Equal(1920, resolved.Left);
        Assert.Equal(80, resolved.Top);
        Assert.Equal(1200, resolved.Width);
        Assert.Equal(700, resolved.Height);
        Assert.Equal(WindowState.Normal, resolved.State);
    }

    [Fact]
    public void WindowPlacementPolicy_Resolve_RecentersWhenSavedMonitorIsGone()
    {
        WindowPlacementSnapshot saved = new(2560, 100, 1280, 800, WindowState.Normal, true);
        Rect primary = new(0, 0, 1920, 1080);

        WindowPlacementSnapshot resolved = WindowPlacementPolicy.Resolve(saved, [primary], primary);

        Assert.Equal((1920 - 1280) / 2.0, resolved.Left);
        Assert.Equal((1080 - 800) / 2.0, resolved.Top);
        Assert.Equal(1280, resolved.Width);
        Assert.Equal(800, resolved.Height);
    }

    [Fact]
    public void WindowPlacementPolicy_Resolve_ClampsSizeToFallbackWorkAreaWhenRelocating()
    {
        WindowPlacementSnapshot saved = new(4000, 0, 2000, 1200, WindowState.Maximized, true);
        Rect primary = new(0, 0, 1366, 768);

        WindowPlacementSnapshot resolved = WindowPlacementPolicy.Resolve(saved, [primary], primary);

        Assert.Equal(1366, resolved.Width);
        Assert.Equal(768, resolved.Height);
        Assert.Equal(0, resolved.Left);
        Assert.Equal(0, resolved.Top);
        Assert.Equal(WindowState.Maximized, resolved.State);
    }

    [Fact]
    public void WindowPlacementPolicy_Write_StoresNormalBoundsAndDropsMinimizedState()
    {
        ApplicationConfiguration config = new();

        WindowPlacementPolicy.Write(
            config,
            WindowState.Minimized,
            new Rect(40, 50, 1300, 720));

        Assert.Equal(40, config.WindowLeft);
        Assert.Equal(50, config.WindowTop);
        Assert.Equal(1300, config.WindowWidth);
        Assert.Equal(720, config.WindowHeight);
        Assert.Equal(WindowState.Normal, config.WindowState);
    }

    [Fact]
    public void WindowPlacementPolicy_IsVisibleOnAMonitor_FalseWhenFullyOffDisplays()
    {
        Rect window = new(4000, 2000, 1100, 650);
        Rect primary = new(0, 0, 1920, 1080);

        Assert.False(WindowPlacementPolicy.IsVisibleOnAMonitor(window, [primary]));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void ShouldHideInsteadOfClose_MatchesPolicy(bool minimizeToTray, bool isExiting, bool expected)
    {
        Assert.Equal(expected, TrayWindowPolicy.ShouldHideInsteadOfClose(minimizeToTray, isExiting));
    }

    [Theory]
    [InlineData(true, WindowState.Minimized, true)]
    [InlineData(true, WindowState.Normal, false)]
    [InlineData(true, WindowState.Maximized, false)]
    [InlineData(false, WindowState.Minimized, false)]
    public void ShouldHideOnMinimize_MatchesPolicy(bool minimizeToTray, WindowState newState, bool expected)
    {
        Assert.Equal(expected, TrayWindowPolicy.ShouldHideOnMinimize(minimizeToTray, newState));
    }

    [Fact]
    public void RestartAsAdminFlow_LaunchFailureReacquiresAndDoesNotExit()
    {
        List<string> steps = new();

        bool completed = RestartAsAdminFlow.Run(
            () =>
            {
                steps.Add("launch");
                throw new InvalidOperationException("elevated launch failed");
            },
            () => steps.Add("release"),
            () => steps.Add("reacquire"),
            () => steps.Add("shutdown"),
            () =>
            {
                steps.Add("wait");
                return true;
            });

        Assert.False(completed);
        Assert.Equal(new[] { "launch", "reacquire" }, steps);
    }

    [Fact]
    public void RestartAsAdminFlow_SuccessfulHandoffReleasesAfterSuccessorIsReady()
    {
        List<string> steps = new();

        bool completed = RestartAsAdminFlow.Run(
            () => steps.Add("launch"),
            () => steps.Add("release"),
            () => steps.Add("reacquire"),
            () => steps.Add("shutdown"),
            () =>
            {
                steps.Add("wait");
                return true;
            });

        Assert.True(completed);
        Assert.Equal(new[] { "launch", "wait", "release", "shutdown" }, steps);
    }

    [Fact]
    public void RestartAsAdminFlow_SuccessorWaitFailureReacquiresAndDoesNotExit()
    {
        List<string> steps = new();

        bool completed = RestartAsAdminFlow.Run(
            () => steps.Add("launch"),
            () => steps.Add("release"),
            () => steps.Add("reacquire"),
            () => steps.Add("shutdown"),
            () =>
            {
                steps.Add("wait");
                return false;
            });

        Assert.False(completed);
        Assert.Equal(new[] { "launch", "wait", "reacquire" }, steps);
    }

    [Fact]
    public async Task SingleInstanceLifetime_HandoffKeepsCompetingStartupFromBecomingPrimary()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string mutexName = "Nefarius.DsHidMini.ControlApp.Tests.Mutex." + suffix;
        string eventName = "Nefarius.DsHidMini.ControlApp.Tests.Event." + suffix;
        string token = Guid.NewGuid().ToString("N");

        using SingleInstanceLifetime parent = new(mutexName, eventName);
        using EventWaitHandle ready = SingleInstanceLifetime.CreateHandoffReadyEvent(token);

        Task<SingleInstanceLifetime?> successorTask = Task.Run(() =>
            SingleInstanceLifetime.TryAdoptAfterHandoff(
                mutexName,
                eventName,
                token,
                TimeSpan.FromSeconds(5)));

        Assert.True(ready.WaitOne(TimeSpan.FromSeconds(5)));

        using (SingleInstanceLifetime competitor = new(mutexName, eventName))
        {
            Assert.False(competitor.IsPrimary);
        }

        parent.ReleaseOwnership();

        SingleInstanceLifetime? successor = await successorTask;
        Assert.NotNull(successor);
        using (successor)
        {
            Assert.True(successor.IsPrimary);

            using SingleInstanceLifetime lateCompetitor = new(mutexName, eventName);
            Assert.False(lateCompetitor.IsPrimary);
        }
    }

    [Fact]
    public void SingleInstanceLifetime_ReleasedParentAllowsReplacementToBecomePrimary()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string mutexName = "Nefarius.DsHidMini.ControlApp.Tests.Mutex." + suffix;
        string eventName = "Nefarius.DsHidMini.ControlApp.Tests.Event." + suffix;

        using SingleInstanceLifetime parent = new(mutexName, eventName);
        Assert.True(parent.IsPrimary);

        using (SingleInstanceLifetime blocked = new(mutexName, eventName))
        {
            Assert.False(blocked.IsPrimary);
        }

        parent.ReleaseOwnership();

        using SingleInstanceLifetime replacement = new(mutexName, eventName);
        Assert.True(replacement.IsPrimary);
    }

    [Fact]
    public void SingleInstanceLifetime_SecondarySetSurvivesSecondaryDispose()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string mutexName = "Nefarius.DsHidMini.ControlApp.Tests.Mutex." + suffix;
        string eventName = "Nefarius.DsHidMini.ControlApp.Tests.Event." + suffix;

        using SingleInstanceLifetime primary = new(mutexName, eventName);
        Assert.True(primary.IsPrimary);

        using (SingleInstanceLifetime secondary = new(mutexName, eventName))
        {
            Assert.False(secondary.IsPrimary);
            secondary.ShowWindowEvent.Set();
        }

        Assert.True(primary.ShowWindowEvent.WaitOne(TimeSpan.FromSeconds(1)));
    }
}
