using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

namespace Nefarius.DsHidMini.Setup.Dialogs
{
    /// <summary>
    /// The standard InstallDirDialog.
    /// </summary>
    /// <seealso cref="WixSharp.UI.WPF.WpfDialog" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    /// <seealso cref="WixSharp.IWpfDialog" />
    public partial class InstallDirDialog : WpfDialog, IWpfDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstallDirDialog"/> class.
        /// </summary>
        public InstallDirDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is invoked by WixSHarp runtime when the custom dialog content is internally fully initialized.
        /// This is a convenient place to do further initialization activities (e.g. localization).
        /// </summary>
        public void Init()
        {
            DataContext = model = new InstallDirDialogModel { Host = ManagedFormHost, };
        }

        InstallDirDialogModel model;

        void GoPrev_Click(object sender, RoutedEventArgs e)
            => model.GoPrev();

        void GoNext_Click(object sender, RoutedEventArgs e)
            => model.GoNext();

        void Cancel_Click(object sender, RoutedEventArgs e)
            => model.Cancel();

        void ChangeInstallDir_Click(object sender, RoutedEventArgs e)
            => model.ChangeInstallDir();
    }

    /// <summary>
    /// ViewModel for standard InstallDirDialog.
    /// </summary>
    internal class InstallDirDialogModel : NotifyPropertyChangedBase
    {
        public ManagedForm Host;
        ISession session => Host?.Runtime.Session;
        IManagedUIShell shell => Host?.Shell;

        public BitmapImage Banner => session?.GetResourceBitmap("WixSharpUI_Bmp_Banner").ToImageSource() ??
                                     session?.GetResourceBitmap("WixUI_Bmp_Banner").ToImageSource();

        string installDirProperty => session?.Property("WixSharp_UI_INSTALLDIR");

        public string InstallDirPath
        {
            get
            {
                if (Host != null)
                {
                    string installDirPropertyValue = session.Property(installDirProperty);

                    if (installDirPropertyValue.IsEmpty())
                    {
                        // We are executed before any of the MSI actions are invoked so the INSTALLDIR (if set to absolute path)
                        // is not resolved yet. So we need to do it manually
                        var installDir = session.GetDirectoryPath(installDirProperty);

                        if (installDir == "ABSOLUTEPATH")
                            installDir = session.Property("INSTALLDIR_ABSOLUTEPATH");

                        return installDir;
                    }
                    else
                    {
                        //INSTALLDIR set either from the command line or by one of the early setup events (e.g. UILoaded)
                        return installDirPropertyValue;
                    }
                }
                else
                    return null;
            }

            set
            {
                session[installDirProperty] = value;
                base.NotifyOfPropertyChange(nameof(InstallDirPath));
            }
        }

        public void ChangeInstallDir()
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = InstallDirPath })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    InstallDirPath = dialog.SelectedPath;
            }
        }

        public void GoPrev()
            => shell?.GoPrev();

        public void GoNext()
            => shell?.GoNext();

        public void Cancel()
            => shell?.Cancel();
    }
}