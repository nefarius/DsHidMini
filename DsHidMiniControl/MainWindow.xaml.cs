using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using DsHidMiniControl.Util;
using NNanomsg.Protocols;

namespace DsHidMiniControl
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DeviceNotificationListener _devListener = new DeviceNotificationListener();

        public MainWindow()
        {
            InitializeComponent();

            

            /*Task.Run(() =>
            {
                using (var s = new SubscribeSocket())
                {
                    //Needs to match the first portion of the message being received.
                    s.Subscribe("DSHM");
                    s.Connect("tcp://192.168.2.113:46856");
                    while (true)
                    {
                        var b = s.Receive();
                        if (b != null)
                        {
                            var pack = DS_PUB_SOCKET_PACKET.FromByteArray(b);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Log.Text = BitConverter.ToString(b).Replace("-", string.Empty);
                            });
                        }
                    }
                }
            });*/
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DS_PUB_SOCKET_PACKET_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Topic;

            public ushort ProtocolVersion;

            public ushort PayloadLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DS3_INPUT_REPORT
        {
            public ushort ButtonState; // Main buttons
            public byte PSButtonState; // PS button
            public byte LeftStickX; // left Joystick X axis 0 - 255, 128 is mid
            public byte LeftStickY; // left Joystick Y axis 0 - 255, 128 is mid
            public byte RightStickX; // right Joystick X axis 0 - 255, 128 is mid
            public byte RightStickY; // right Joystick Y axis 0 - 255, 128 is mid
            public byte PressureUp; // digital Pad Up button Pressure 0 - 255
            public byte PressureRight; // digital Pad Right button Pressure 0 - 255
            public byte PressureDown; // digital Pad Down button Pressure 0 - 255
            public byte PressureLeft; // digital Pad Left button Pressure 0 - 255
            public byte PressureL2; // digital Pad L2 button Pressure 0 - 255
            public byte PressureR2; // digital Pad R2 button Pressure 0 - 255
            public byte PressureL1; // digital Pad L1 button Pressure 0 - 255
            public byte PressureR1; // digital Pad R1 button Pressure 0 - 255
            public byte PressureTriangle; // digital Pad Triangle button Pressure 0 - 255
            public byte PressureCircle; // digital Pad Circle button Pressure 0 - 255
            public byte PressureCross; // digital Pad Cross button Pressure 0 - 255
            public byte PressureSquare; // digital Pad Square button Pressure 0 - 255
            public ushort AccelerometerX; // X axis accelerometer Big Endian 0 - 1023
            public ushort AccelerometerY; // Y axis accelerometer Big Endian 0 - 1023
            public ushort AccelerometerZ; // Z axis accelerometer Big Endian 0 - 1023
            public ushort GyrometerX; // Z axis Gyro Big Endian 0 - 1023
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DS_PUB_SOCKET_PACKET
        {
            public DS_PUB_SOCKET_PACKET_HEADER Header;

            //
            // Type of connection (wired, wireless)
            // 
            public uint ConnectionType;

            //
            // Remote BTH address
            // 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] HostAddress;

            //
            // Local device BTH address
            // 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] DeviceAddress;

            //
            // Current reported battery status
            // 
            public uint BatteryStatus;

            public uint HidDeviceMode;

            public ushort VendorId;

            public ushort ProductId;

            public ushort VersionNumber;

            public DS3_INPUT_REPORT InputReport;

            public static DS_PUB_SOCKET_PACKET FromByteArray(byte[] array)
            {
                var str = new DS_PUB_SOCKET_PACKET();

                var size = Marshal.SizeOf(str);
                var ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(array, 0, ptr, size);

                str = (DS_PUB_SOCKET_PACKET) Marshal.PtrToStructure(ptr, str.GetType());
                Marshal.FreeHGlobal(ptr);

                return str;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _devListener.StartListen(this);
        }
    }
}