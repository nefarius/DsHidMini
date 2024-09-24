using System.Windows.Media.Imaging;

using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

namespace Nefarius.DsHidMini.Setup.Dialogs
{
    /// <summary>
    /// The standard MaintenanceTypeDialog.
    /// </summary>
    /// <seealso cref="WixSharp.UI.WPF.WpfDialog" />
    /// <seealso cref="WixSharp.IWpfDialog" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    public partial class MaintenanceTypeDialog : WpfDialog, IWpfDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaintenanceTypeDialog" /> class.
        /// </summary>
        public MaintenanceTypeDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is invoked by WixSHarp runtime when the custom dialog content is internally fully initialized.
        /// This is a convenient place to do further initialization activities (e.g. localization).
        /// </summary>
        public void Init()
        {
            DataContext = model = new MaintenanceTypeDialogModel { Host = ManagedFormHost };
        }

        MaintenanceTypeDialogModel model;

        void GoPrev_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.GoPrev();

        void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.Cancel();

        void Remove_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.Remove();

        void Repair_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.Repair();

        void Change_Click(object sender, System.Windows.RoutedEventArgs e)
            => model.Change();
    }

    /// <summary>
    /// ViewModel for standard MaintenanceTypeDialog.
    /// </summary>
    internal class MaintenanceTypeDialogModel : NotifyPropertyChangedBase
    {
        public ManagedForm Host;

        ISession session => Host?.Runtime.Session;
        IManagedUIShell shell => Host?.Shell;

        public BitmapImage Banner => session?.GetResourceBitmap("WixSharpUI_Bmp_Banner").ToImageSource() ??
                                     session?.GetResourceBitmap("WixUI_Bmp_Banner").ToImageSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="MaintenanceTypeDialog" /> class.
        /// </summary>
        void JumpToProgressDialog()
        {
            int index = shell.Dialogs.IndexOfDialogImplementing<IProgressDialog>();
            if (index != -1)
                shell.GoTo(index);
            else
                shell.GoNext(); // if user did not supply progress dialog then simply go next
        }

        public void Change()
        {
            if (session != null)
            {
                session["MODIFY_ACTION"] = "Change";
                shell.GoNext();
            }
        }

        public void Repair()
        {
            if (session != null)
            {
                session["MODIFY_ACTION"] = "Repair";
                JumpToProgressDialog();
            }
        }

        public void Remove()
        {
            if (session != null)
            {
                session["REMOVE"] = "ALL";
                session["MODIFY_ACTION"] = "Remove";

                JumpToProgressDialog();
            }
        }

        public void GoPrev()
            => shell?.GoPrev();

        public void Cancel()
            => Host?.Shell.Cancel();
    }
}