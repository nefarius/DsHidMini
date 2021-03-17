using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace Nefarius.DsHidMini.UI.Troubleshooting
{
    /// <summary>
    ///     Interaction logic for TraceSessionView.xaml
    /// </summary>
    public partial class TraceSessionView : UserControl
    {
        private bool isTraceRunning;

        public TraceSessionView()
        {
            InitializeComponent();
        }

        private async void ToggleTrace_OnClick(object sender, RoutedEventArgs e)
        {
            //
            // TODO: tidy up, error handling etc.
            // 

            var button = (Button) sender;

            button.IsEnabled = !button.IsEnabled;
            button.Content = "Thinking...";

            var sessionName = "DsHidMini";
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var traceFileName = $@"{DateTime.Now.Ticks}.etl";
            var traceFile = Path.Combine(appPath, traceFileName);

            Task task;

            if (!isTraceRunning)
            {
                task = Task.Factory.StartNew(() =>
                {
                    using (var inst = PowerShell.Create())
                    {
                        inst.AddScript("Import-Module EventTracingManagement");
                        inst.AddScript(
                            $"New-EtwTraceSession -Name {sessionName} -LogFileMode 0x8100 -FlushTimer 1 -LocalFilePath \"{traceFile}\"");
                        inst.AddScript(
                            $"Add-EtwTraceProvider -SessionName {sessionName} -Guid '{{37dcd579-e844-4c80-9c8b-a10850b6fac6}}' -MatchAnyKeyword 0x0FFFFFFFFFFFFFFF -Level 0xFF -Property 0x40");
                        inst.AddScript(
                            $"Add-EtwTraceProvider -SessionName {sessionName} -Guid '{{586aa8b1-53a6-404f-9b3e-14483e514a2c}}' -MatchAnyKeyword 0x0FFFFFFFFFFFFFFF -Level 0xFF -Property 0x40");
                        inst.AddScript(
                            $"Add-EtwTraceProvider -SessionName {sessionName} -Guid '{{A56A946C-AC5C-4E2F-9179-6821272856C6}}' -MatchAnyKeyword 0x0FFFFFFFFFFFFFFF -Level 0xFF -Property 0x40");

                        return inst.Invoke();
                    }
                });

                // For error handling.
                await task.ContinueWith(t =>
                {
                    Log.Error("Starting trace failed: {Exception}", t.Exception);
                });

                isTraceRunning = !isTraceRunning;

                button.Content = "Stop trace";
            }
            else
            {
                task = Task.Factory.StartNew(() =>
                {
                    using (var inst = PowerShell.Create())
                    {
                        inst.AddScript("Import-Module EventTracingManagement");
                        inst.AddScript($"Remove-EtwTraceSession -Name {sessionName}");

                        return inst.Invoke();
                    }
                });

                // For error handling.
                await task.ContinueWith(t =>
                {
                    Log.Error("Stopping trace failed: {Exception}", t.Exception);
                });

                isTraceRunning = !isTraceRunning;

                button.Content = "Start trace";
            }

            button.IsEnabled = !button.IsEnabled;
        }
    }
}