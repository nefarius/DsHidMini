using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

using IO = System.IO;

namespace Nefarius.DsHidMini.Setup.Dialogs
{
    /// <summary>
    /// The standard ExitDialog.
    /// </summary>
    /// <seealso cref="WixSharp.UI.WPF.WpfDialog" />
    /// <seealso cref="WixSharp.IWpfDialog" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class ExitDialog : WpfDialog, IWpfDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExitDialog"/> class.
        /// </summary>
        public ExitDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is invoked by WixSHarp runtime when the custom dialog content is internally fully initialized.
        /// This is a convenient place to do further initialization activities (e.g. localization).
        /// </summary>
        public void Init()
        {
            UpdateTitles(ManagedFormHost.Runtime.Session);

            DataContext = model = new ExitDialogModel { Host = ManagedFormHost };
        }

        ExitDialogModel model;

        /// <summary>
        /// Updates the titles of the dialog depending on the success of the installation action.
        /// </summary>
        /// <param name="session">The session.</param>
        public void UpdateTitles(ISession session)
        {
            if (Shell.UserInterrupted || Shell.Log.Contains("User canceled installation."))
            {
                DialogTitleLabel.Text = "[UserExitTitle]";
                DialogDescription.Text = "[UserExitDescription1]";
            }
            else if (Shell.ErrorDetected)
            {
                DialogTitleLabel.Text = "[FatalErrorTitle]";
                DialogDescription.Text = Shell.CustomErrorDescription ?? "[FatalErrorDescription1]";
            }

            // `Localize` resolves [...] titles and descriptions into the localized strings stored in MSI resources tables
            this.Localize();
        }

        void ViewLog_Click(object sender, RoutedEventArgs e)
            => model.ViewLog();

        void GoExit_Click(object sender, RoutedEventArgs e)
            => model.GoExit();

        void Cancel_Click(object sender, RoutedEventArgs e)
            => model.Cancel();
    }

    /// <summary>
    /// ViewModel for standard ExitDialog.
    /// </summary>
    internal class ExitDialogModel
    {
        public ManagedForm Host { get; set; }
        ISession session => Host?.Runtime.Session;
        IManagedUIShell shell => Host?.Shell;

        public BitmapImage Banner => session?.GetResourceBitmap("WixSharpUI_Bmp_Dialog").ToImageSource() ??
                                     session?.GetResourceBitmap("WixUI_Bmp_Dialog").ToImageSource();

        public void GoExit()
            => shell?.Exit();

        public void Cancel()
            => shell?.Exit();

        public void ViewLog()
        {
            if (shell != null)
                try
                {
                    string logFile = session.LogFile;

                    if (logFile.IsEmpty())
                    {
                        string wixSharpDir = Path.GetTempPath().PathCombine("WixSharp");

                        if (!Directory.Exists(wixSharpDir))
                            Directory.CreateDirectory(wixSharpDir);

                        logFile = wixSharpDir.PathCombine(Host.Runtime.ProductName + ".log");
                        IO.File.WriteAllText(logFile, shell.Log);
                    }
                    Process.Start("notepad.exe", logFile);
                }
                catch
                {
                    // Catch all, we don't want the installer to crash in an
                    // attempt to view the log.
                }
        }
    }
}