namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Pair-now sequence: persist pairing preferences best-effort, then always
///     send the controller pair request. A denied config write must not suppress IPC.
/// </summary>
internal static class PairingRequestWorkflow
{
    public static PairingRequestWorkflowResult<T> PersistThenPair<T>(
        Func<bool> persistPreferences,
        Func<T> sendPairRequest)
    {
        ArgumentNullException.ThrowIfNull(persistPreferences);
        ArgumentNullException.ThrowIfNull(sendPairRequest);

        bool persistSucceeded = persistPreferences();
        T pairResult = sendPairRequest();
        return new PairingRequestWorkflowResult<T>(persistSucceeded, pairResult);
    }
}

internal readonly record struct PairingRequestWorkflowResult<T>(bool PersistSucceeded, T PairResult);
