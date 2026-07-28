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

    /// <summary>
    /// Initializes a new instance of OnlineDocumentationDialog and initializes its WPF UI components.
    /// </summary>
    public OnlineDocumentationDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the dialog's view model and sets it as the dialog's DataContext.
    /// </summary>
    /// <remarks>
    /// Creates a new <see cref="OnlineDocumentationDialogModel"/>, assigns <see cref="ManagedFormHost"/> to its <c>Host</c> property, stores it in the backing field, and sets <c>DataContext</c> to the created model.
    /// </remarks>
    public void Init()
    {
        DataContext = _model = new OnlineDocumentationDialogModel { Host = ManagedFormHost };
    }

    /// <summary>
    /// Handles the Next button click and advances the installer to the next dialog.
    /// </summary>
    /// <param name="sender">The control that raised the click event.</param>
    /// <param name="e">Event data for the routed event.</param>
    private void GoNext_Click(object sender, RoutedEventArgs e)
    {
        _model.GoNext();
    }

    /// <summary>
    /// Attempts to open the application's online documentation URL in the user's default web browser.
    /// </summary>
    /// <remarks>
    /// Any exception thrown while launching the browser is caught and ignored so setup does not fail if the browser cannot be started.
    /// </remarks>
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

    /// <summary>
    /// Advances the installer to the next step in the dialog sequence.
    /// </summary>
    /// <remarks>
    /// If a hosting shell is available, requests it to navigate to the next page; otherwise does nothing.
    /// </remarks>
    public void GoNext()
    {
        Shell?.GoNext();
    }
}
