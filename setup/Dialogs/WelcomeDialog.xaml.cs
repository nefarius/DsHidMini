using System.Windows.Media.Imaging;

using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

namespace Nefarius.DsHidMini.Setup.Dialogs
{
    /// <summary>
    /// The standard WelcomeDialog.
    /// </summary>
    /// <seealso cref="WixSharp.UI.WPF.WpfDialog" />
    /// <seealso cref="WixSharp.IWpfDialog" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class WelcomeDialog : WpfDialog, IWpfDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeDialog" /> class.
        /// </summary>
        public WelcomeDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is invoked by WixSHarp runtime when the custom dialog content is internally fully initialized.
        /// This is a convenient place to do further initialization activities (e.g. localization).
        /// </summary>
        public void Init()
        {
            this.DataContext = model = new WelcomeDialogModel { Host = ManagedFormHost };
        }

        WelcomeDialogModel model;

        void GoPrev_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.GoPrev();

        void GoNext_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.GoNext();

        void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.Cancel();
    }

    /// <summary>
    /// ViewModel for standard WelcomeDialog.
    /// </summary>
    internal class WelcomeDialogModel : NotifyPropertyChangedBase
    {
        public ManagedForm Host;
        ISession session => Host?.Runtime.Session;
        IManagedUIShell shell => Host?.Shell;

        public BitmapImage Banner => session?.GetResourceBitmap("WixSharpUI_Bmp_Dialog")?.ToImageSource() ??
                                     session?.GetResourceBitmap("WixUI_Bmp_Dialog")?.ToImageSource();

        public bool CanGoPrev => false;

        public void GoPrev()
            => shell?.GoPrev();

        public void GoNext()
            => shell?.GoNext();

        public void Cancel()
            => shell?.Cancel();
    }
}