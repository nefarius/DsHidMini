#nullable enable
using System;
using System.Linq;

using Microsoft.Deployment.WindowsInstaller;

namespace Nefarius.DsHidMini.Setup.Util;

public static class SessionExtensions
{
    public static bool IsFeatureEnabledPartial(this Session session, string partialName)
    {
        FeatureInfo? featureInfo =
            session.Features.SingleOrDefault(f => f.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));

        if (featureInfo is null)
        {
            return false;
        }

        return featureInfo.RequestState != InstallState.Unknown;
    }
}