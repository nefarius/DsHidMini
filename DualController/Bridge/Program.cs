namespace DualController.Bridge;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            string file = args.FirstOrDefault(a => a.StartsWith("--diagnostics-file=", StringComparison.OrdinalIgnoreCase))?
                ["--diagnostics-file=".Length..] ?? Path.Combine(Path.GetTempPath(), "dual-controller-hid-check.json");
            try
            {
                HidDevices.ValidateLayouts();
                var devices = HidDevices.Enumerate();
                File.WriteAllText(file, System.Text.Json.JsonSerializer.Serialize(new
                { Passed = true, PhysicalPs4Count = devices.Count, Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString() }));
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText(file, System.Text.Json.JsonSerializer.Serialize(new { Passed = false, Error = ex.ToString() }));
                return 1;
            }
        }
        using var singleInstance = new Mutex(true, @"Local\headd16.DualController.Bridge", out bool created);
        if (!created)
        {
            MessageBox.Show("Dual Controller is already running. Open it from the system tray.", "Dual Controller");
            return 0;
        }
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => { Log.Write(e.Exception.ToString()); MessageBox.Show(e.Exception.Message, "Dual Controller"); };
        Application.Run(new MainForm(args.Contains("--tray", StringComparer.OrdinalIgnoreCase)));
        return 0;
    }
}
