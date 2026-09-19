using System;

static class OpenArticleTests
{
    public static void Run()
    {
        AssertLaunch(false, "0x1234", false, "full UI, PostInstArticle disabled");
        AssertLaunch(true, "", false, "no Managed UI handle, PostInstArticle disabled");
        AssertLaunch(true, "0x1234", true, "PostInstArticle enabled");
    }

    static void AssertLaunch(bool expected, string handle, bool featureEnabled, string name)
    {
        bool actual = Nefarius.DsHidMini.Setup.OpenArticleDecision.ShouldLaunch(handle, featureEnabled);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"FAIL {name}: expected launch={expected}, got {actual}.");
        }
    }
}
