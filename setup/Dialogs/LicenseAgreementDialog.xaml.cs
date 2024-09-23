using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

using IO = System.IO;

namespace Nefarius.DsHidMini.Setup.Dialogs
{
    /// <summary>
    /// The standard LicenceDialog.
    /// </summary>
    /// <seealso cref="WixSharp.UI.WPF.WpfDialog" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    /// <seealso cref="WixSharp.IWpfDialog" />
    public partial class LicenceDialog : WpfDialog, IWpfDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LicenceDialog"/> class.
        /// </summary>
        public LicenceDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is invoked by WixSHarp runtime when the custom dialog content is internally fully initialized.
        /// This is a convenient place to do further initialization activities (e.g. localization).
        /// </summary>
        public void Init()
        {
            DataContext = model = new LicenseDialogModel
            {
                ShowRtfContent = x => this.LicenceText.SetRtf(x),
                Host = ManagedFormHost,
            };
        }

        LicenseDialogModel model;

        void GoPrev_Click(object sender, RoutedEventArgs e)
            => model.GoPrev();

        void GoNext_Click(object sender, RoutedEventArgs e)
            => model.GoNext();

        void Cancel_Click(object sender, RoutedEventArgs e)
            => model.Cancel();

        void Print_Click(object sender, RoutedEventArgs e)
            => model.Print();
    }

    static partial class Extension
    {
        public static void SetRtf(this RichTextBox rtb, string document)
        {
            var documentBytes = Encoding.UTF8.GetBytes(document);
            using (var reader = new MemoryStream(documentBytes))
            {
                reader.Position = 0;
                rtb.SelectAll();
                rtb.Selection.Load(reader, DataFormats.Rtf);
            }
        }
    }

    /// <summary>
    /// ViewModel for standard LicenceDialog.
    /// </summary>
    internal class LicenseDialogModel : NotifyPropertyChangedBase
    {
        ManagedForm host;
        ISession session => Host?.Runtime.Session;
        IManagedUIShell shell => Host?.Shell;

        public Action<string> ShowRtfContent;

        public ManagedForm Host
        {
            get => host;
            set
            {
                host = value;

                ShowRtfContent?.Invoke(LicenceText);
                NotifyOfPropertyChange(nameof(Banner));
                NotifyOfPropertyChange(nameof(LicenseAcceptedChecked));
                NotifyOfPropertyChange(nameof(CanGoNext));
            }
        }

        public string LicenceText => session?.GetResourceString("WixSharp_LicenceFile");

        public BitmapImage Banner => session?.GetResourceBitmap("WixSharpUI_Bmp_Banner").ToImageSource() ??
                                     session?.GetResourceBitmap("WixUI_Bmp_Banner").ToImageSource();

        public bool LicenseAcceptedChecked
        {
            get => session?["LastLicenceAcceptedChecked"] == "True";
            set
            {
                if (Host != null)
                    session["LastLicenceAcceptedChecked"] = value.ToString();

                NotifyOfPropertyChange(nameof(LicenseAcceptedChecked));
                NotifyOfPropertyChange(nameof(CanGoNext));
            }
        }

        public bool CanGoNext
            => LicenseAcceptedChecked;

        public void GoPrev()
            => shell?.GoPrev();

        public void GoNext()
            => shell?.GoNext();

        public void Cancel()
            => shell?.Cancel();

        public void Print()
        {
            try
            {
                var file = IO.Path.GetTempPath().PathCombine(Host?.Runtime.Session.Property("ProductName") + ".licence.rtf");
                IO.File.WriteAllText(file, LicenceText);
                Process.Start(file);
            }
            catch
            {
                // Catch all, we don't want the installer to crash in an
                // attempt to write to a file.
            }
        }
    }
}