using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

using IO = System.IO;

namespace Nefarius.DsHidMini.Setup.Dialogs;

/// <summary>
/// The standard LicenceDialog.
/// </summary>
/// <seealso cref="WixSharp.UI.WPF.WpfDialog" />
/// <seealso cref="System.Windows.Markup.IComponentConnector" />
/// <seealso cref="WixSharp.IWpfDialog" />
public partial class BetaArticleDialog : WpfDialog, IWpfDialog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LicenceDialog"/> class.
    /// </summary>
    public BetaArticleDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// This method is invoked by WixSHarp runtime when the custom dialog content is internally fully initialized.
    /// This is a convenient place to do further initialization activities (e.g. localization).
    /// </summary>
    public void Init()
    {
        DataContext = _model = new BetaArticleDialogModel { Host = ManagedFormHost };
    }

    private BetaArticleDialogModel _model;

    private void GoNext_Click(object sender, RoutedEventArgs e)
    {
        _model.GoNext();
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(InstallScript.BetaArticleUrl.ToString());
        }
        catch
        {
            // welp
        }
    }
}

/// <summary>
/// ViewModel for standard LicenceDialog.
/// </summary>
internal class BetaArticleDialogModel : NotifyPropertyChangedBase
{
    private ManagedForm _host;
    private ISession Session => Host?.Runtime.Session;
    private IManagedUIShell Shell => Host?.Shell;
    
    public ManagedForm Host
    {
        get => _host;
        set
        {
            _host = value;

            NotifyOfPropertyChange(nameof(Banner));
            NotifyOfPropertyChange(nameof(CanGoNext));
        }
    }

    public BitmapImage Banner => Session?.GetResourceBitmap("WixSharpUI_Bmp_Banner").ToImageSource() ??
                                 Session?.GetResourceBitmap("WixUI_Bmp_Banner").ToImageSource();


    public bool CanGoNext
        => true;

    public void GoNext()
    {
        Shell?.GoNext();
    }
}