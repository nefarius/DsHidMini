using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class RawInputReportButtonTests
{
    [Fact]
    public void ReportAndButtonUnion_KeepFourByteButtonLayout()
    {
        Assert.Equal(4, Marshal.SizeOf<DS3_RAW_INPUT_REPORT.ButtonUnion>());
        Assert.Equal(49, Marshal.SizeOf<DS3_RAW_INPUT_REPORT>());
    }

    [Fact]
    public void PsButton_ReadsAndWritesBitSixteen()
    {
        DS3_RAW_INPUT_REPORT report = default;

        Assert.False(report.Buttons.PS);

        report.Buttons.lButtons = 1u << 16;

        Assert.True(report.Buttons.PS);
        Assert.False(report.Buttons.Square);

        report.Buttons.PS = false;

        Assert.False(report.Buttons.PS);
        Assert.Equal(0u, report.Buttons.lButtons);

        report.Buttons.PS = true;

        Assert.True(report.Buttons.PS);
        Assert.Equal(1u << 16, report.Buttons.lButtons);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    public void DigitalBits_RoundTripThroughPackedButtons(int bit)
    {
        DS3_RAW_INPUT_REPORT report = default;
        report.Buttons.lButtons = 1u << bit;

        bool[] bits =
        [
            report.Buttons.Select,
            report.Buttons.L3,
            report.Buttons.R3,
            report.Buttons.Start,
            report.Buttons.Up,
            report.Buttons.Right,
            report.Buttons.Down,
            report.Buttons.Left,
            report.Buttons.L2,
            report.Buttons.R2,
            report.Buttons.L1,
            report.Buttons.R1,
            report.Buttons.Triangle,
            report.Buttons.Circle,
            report.Buttons.Cross,
            report.Buttons.Square
        ];

        for (int i = 0; i < bits.Length; i++)
        {
            Assert.Equal(i == bit, bits[i]);
        }
    }
}
