using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

namespace Nefarius.DsHidMini.Setup.Dialogs;

public partial class OnlineDocumentationDialog : WpfDialog, IWpfDialog
{
    private OnlineDocumentationDialogModel _model;

    public OnlineDocumentationDialog()
    {
        InitializeComponent();
    }

    public void Init()
    {
        DataContext = _model = new OnlineDocumentationDialogModel { Host = ManagedFormHost };
    }

    private void GoNext_Click(object sender, RoutedEventArgs e)
    {
        _model.GoNext();
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(InstallScript.OnlineDocumentationUrl.ToString());
        }
        catch
        {
            // Not failing setup because a browser couldn't be launched
        }
    }
}

internal class OnlineDocumentationDialogModel : NotifyPropertyChangedBase
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

    public bool CanGoNext => true;

    public void GoNext()
    {
        Shell?.GoNext();
    }
}
