using System.Diagnostics;

namespace DualController.Bridge;

internal sealed class MainForm : Form
{
    private readonly ControllerManager manager = new();
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 750 };
    private readonly ListView controllers = new() { View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill };
    private readonly Label status = new() { AutoSize = false, Dock = DockStyle.Fill };
    private readonly NotifyIcon tray;
    private readonly bool startHidden;
    private bool exiting;

    public MainForm(bool startHidden)
    {
        this.startHidden = startHidden;
        Text = "Dual Controller — PS3 + PS4 (experimental 0.1)";
        Size = new Size(940, 520); MinimumSize = new Size(780, 460);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        Icon = SystemIcons.Application;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 6, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.Controls.Add(new Label { Text = "PS3 + PS4 · USB & Bluetooth", Font = new Font(Font, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(new Label { Text = "PS3: use DsHidMini ControlApp and select XInput. PS4: keep this app running for Xbox input.\nPair PS4 with SHARE + PS, then add “Wireless Controller” in Windows Bluetooth settings.", Dock = DockStyle.Fill }, 0, 1);
        foreach (var column in new[] { ("Controller", 155), ("Connection", 100), ("Status", 225), ("Battery", 115), ("Live input", 270) })
            controllers.Columns.Add(column.Item1, column.Item2);
        layout.Controls.Add(controllers, 0, 2);
        layout.Controls.Add(status, 0, 3);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var mapping = new CheckBox { Text = "PS4 Xbox mapping", Checked = true, AutoSize = true, Margin = new Padding(3, 10, 15, 3) };
        mapping.CheckedChanged += (_, _) => manager.Enabled = mapping.Checked;
        buttons.Controls.Add(mapping);
        AddButton(buttons, "PS3 settings", OpenPs3Settings);
        AddButton(buttons, "Bluetooth settings", () => Open("ms-settings:bluetooth"));
        AddButton(buttons, "Logs", () => { Directory.CreateDirectory(Log.DirectoryPath); Open(Log.DirectoryPath); });
        AddButton(buttons, "Exit", () => { exiting = true; Close(); });
        layout.Controls.Add(buttons, 0, 4);
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Close this window to keep mapping in the tray. Use Exit to stop it.\nXbox mapping covers buttons, sticks, triggers and rumble. Touchpad gestures and motion are not mapped.\nIf a game receives input twice, pause Xbox mapping and use its native PS4 support or Steam Input." }, 0, 5);
        Controls.Add(layout);

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open", null, (_, _) => ShowWindow());
        trayMenu.Items.Add("Exit", null, (_, _) => { exiting = true; Close(); });
        tray = new NotifyIcon { Icon = Icon, Text = "Dual Controller — PS3 + PS4", Visible = true, ContextMenuStrip = trayMenu };
        tray.DoubleClick += (_, _) => ShowWindow();
        refresh.Tick += (_, _) => RefreshControllers();
        refresh.Start();
        FormClosing += OnClosing;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (startHidden) Hide();
    }

    private static void AddButton(FlowLayoutPanel panel, string name, Action action)
    {
        var button = new Button { Text = name, AutoSize = true, Height = 32 };
        button.Click += (_, _) => action(); panel.Controls.Add(button);
    }
    private void ShowWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }
    private void RefreshControllers()
    {
        controllers.BeginUpdate(); controllers.Items.Clear();
        foreach (var pad in manager.Snapshot)
            controllers.Items.Add(new ListViewItem([pad.Name, pad.Connection, pad.Status, pad.Battery, pad.Input]));
        controllers.EndUpdate(); status.Text = manager.ScanStatus;
    }

    private static void OpenPs3Settings()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string[] candidates = [Path.Combine(root, "Nefarius Software Solutions", "DsHidMini", "ControlApp.exe"),
            Path.Combine(root, "Nefarius Software Solutions e.U.", "DsHidMini", "ControlApp.exe"),
            Path.Combine(root, "DsHidMini", "ControlApp.exe")];
        string? app = candidates.FirstOrDefault(File.Exists);
        if (app is null) MessageBox.Show("Open DsHidMini ControlApp from the Windows Start menu, then set your PS3 profile to XInput.", "PS3 settings");
        else Open(app);
    }
    private static void Open(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Dual Controller"); }
    }
    private async void OnClosing(object? sender, FormClosingEventArgs args)
    {
        if (args.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing) exiting = true;
        args.Cancel = true;
        if (!exiting) { Hide(); return; }
        Enabled = false; refresh.Stop();
        FormClosing -= OnClosing;
        await manager.DisposeAsync();
        tray.Visible = false; tray.Dispose(); refresh.Dispose();
        Close();
    }
}
