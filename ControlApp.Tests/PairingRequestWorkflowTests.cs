using Nefarius.DsHidMini.ControlApp.Models;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class PairingRequestWorkflowTests
{
    [Fact]
    public void PersistThenPair_WhenPersistFails_StillSendsPairRequest()
    {
        bool pairCalled = false;

        PairingRequestWorkflowResult<string> outcome = PairingRequestWorkflow.PersistThenPair(
            () => false,
            () =>
            {
                pairCalled = true;
                return "paired";
            });

        Assert.False(outcome.PersistSucceeded);
        Assert.True(pairCalled);
        Assert.Equal("paired", outcome.PairResult);
    }

    [Fact]
    public void PersistThenPair_WhenPersistSucceeds_ReturnsPairResult()
    {
        PairingRequestWorkflowResult<int> outcome = PairingRequestWorkflow.PersistThenPair(
            () => true,
            () => 42);

        Assert.True(outcome.PersistSucceeded);
        Assert.Equal(42, outcome.PairResult);
    }
}
