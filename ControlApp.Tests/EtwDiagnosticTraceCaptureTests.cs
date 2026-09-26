using System.Text;

using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class EtwDiagnosticTraceCaptureTests
{
    /// <summary>
    ///     Matches the exact shape <c>Nefarius.Utilities.ETW</c>'s <c>EtwJsonWriter</c> produces for
    ///     one manifest-based event: <c>{"Event":{"Timestamp":...,"ProviderGuid":...,"Id":...,
    ///     "Name":...,"Properties":[{...}]}}</c>.
    /// </summary>
    [Fact]
    public void ParseEvent_DecodesTypedPropertiesFromRealisticPayload()
    {
        string json = $$"""
                        {
                          "Event": {
                            "Timestamp": 133700000000000000,
                            "ProviderGuid": "{{KnownDiagnosticProviders.BthPS3Psm}}",
                            "Id": 9,
                            "Version": 0,
                            "ProcessId": 4,
                            "ThreadId": 8,
                            "ProcessorNumber": 0,
                            "Name": "PsmPatchActivity",
                            "Properties": [
                              {
                                "OriginalPsm": 17,
                                "EffectivePsm": 20563,
                                "Patched": true,
                                "Channel": 1
                              }
                            ]
                          }
                        }
                        """;

        DiagnosticEventRecord? record = EtwDiagnosticTraceCapture.ParseEvent(Encoding.UTF8.GetBytes(json));

        Assert.NotNull(record);
        Assert.Equal(KnownDiagnosticProviders.BthPS3Psm, record!.ProviderGuid);
        Assert.Equal("BthPS3PSM", record.ProviderName);
        Assert.Equal(9, record.EventId);
        Assert.Equal("PsmPatchActivity", record.EventName);
        Assert.Equal(true, record.GetBool("Patched"));
        Assert.Equal(1u, record.GetUInt32("Channel"));
        Assert.Equal(17u, record.GetUInt32("OriginalPsm"));
    }

    [Fact]
    public void ParseEvent_WithoutEventProperty_ReturnsNull()
    {
        DiagnosticEventRecord? record = EtwDiagnosticTraceCapture.ParseEvent(Encoding.UTF8.GetBytes("{}"));

        Assert.Null(record);
    }

    [Fact]
    public void ParseEvent_WithUnknownProviderGuid_StillDecodesUsingRawGuidAsName()
    {
        string json = """
                      {
                        "Event": {
                          "Timestamp": 133700000000000000,
                          "ProviderGuid": "00000000-0000-0000-0000-000000000001",
                          "Id": 1,
                          "Name": "SomeEvent",
                          "Properties": [ {} ]
                        }
                      }
                      """;

        DiagnosticEventRecord? record = EtwDiagnosticTraceCapture.ParseEvent(Encoding.UTF8.GetBytes(json));

        Assert.NotNull(record);
        Assert.Equal("00000000-0000-0000-0000-000000000001", record!.ProviderName);
    }
}
