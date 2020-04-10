using System;
using System.Threading.Tasks;
using System.Windows;
using NNanomsg.Protocols;

namespace DsHidMiniControl
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Task.Run(() =>
            {
                using (var s = new SubscribeSocket())
                {
                    //Needs to match the first portion of the message being received.
                    s.Subscribe("");
                    s.Connect("tcp://127.0.0.1:46856");
                    while (true)
                    {
                        var b = s.Receive();
                        if (b != null)
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Log.Text += BitConverter.ToString(b).Replace("-", string.Empty) +
                                            Environment.NewLine;
                            });
                    }
                }
            });
        }
    }
}