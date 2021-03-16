using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Nefarius.DsHidMini.UI
{
    /// <summary>
    ///     Interaction logic for TitleBarView.xaml
    /// </summary>
    public partial class TitleBarView : UserControl
    {
        public TitleBarView()
        {
            InitializeComponent();
        }

        private void ReleasesHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink) sender).NavigateUri.ToString());
        }

        private void Help_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vigem.org/projects/DsHidMini/");
        }
    }
}