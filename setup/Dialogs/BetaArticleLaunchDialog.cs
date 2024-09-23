using System;
using System.Diagnostics;

using WixSharp;
using WixSharp.UI.Forms;

namespace Nefarius.DsHidMini.Setup.Dialogs;
public partial class BetaArticleLaunchDialog : ManagedForm, IManagedDialog
{
    public BetaArticleLaunchDialog()
    {
        //NOTE: If this assembly is compiled for v4.0.30319 runtime, it may not be compatible with the MSI hosted CLR.
        //The incompatibility is particularly possible for the Embedded UI scenarios. 
        //The safest way to avoid the problem is to compile the assembly for v3.5 Target Framework.WixSharp Setup
        InitializeComponent();
    }

    void dialog_Load(object sender, EventArgs e)
    {
        banner.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Banner");
        Text = "[ProductName] Setup";

        //resolve all Control.Text cases with embedded MSI properties (e.g. 'ProductName') and *.wxl file entries
        base.Localize();
    }
    
    void next_Click(object sender, EventArgs e)
    {
        Shell.GoNext();
    }
    
    private void linkLabelBetaArticle_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(InstallScript.BetaArticleUrl.ToString());
        }
        catch
        {
           // welp, too bad
        }
    }
}