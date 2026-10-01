using System;
using Nefarius.DsHidMini.Setup;

static class RebootPendingTests
{
    public static void Run()
    {
        Version pkg = new(3, 6, 0, 100);

        Assert(false, Decide(false, pkg), "clean");
        Assert(true, Decide(true, pkg), "installer requested");
        Assert(true, Decide(false, pkg, new DevNodeRebootInfo { IsRebootRequired = true }), "IsRebootRequired");
        Assert(true, Decide(false, pkg, new DevNodeRebootInfo { ProblemCode = 14 }), "problem 14");
        Assert(false, Decide(false, pkg, new DevNodeRebootInfo { ProblemCode = 0 }), "problem 0");
        Assert(true, Decide(false, pkg, new DevNodeRebootInfo { DevNodeStatus = 0x100 }), "DN_NEED_RESTART");
        Assert(true, Decide(false, pkg, new DevNodeRebootInfo { BoundDriverVersion = new Version(3, 5, 0, 1) }),
            "older bound driver");
        Assert(false, Decide(false, pkg, new DevNodeRebootInfo { BoundDriverVersion = pkg }), "same version");
        Assert(false, Decide(false, pkg, new DevNodeRebootInfo { BoundDriverVersion = null }), "unreadable version");
    }

    static bool Decide(bool flag, Version pkg, params DevNodeRebootInfo[] devices)
    {
        return RebootPendingDecision.Decide(flag, pkg, devices, out _);
    }

    static void Assert(bool expected, bool actual, string name)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"FAIL {name}: expected {expected}, got {actual}.");
        }
    }
}
