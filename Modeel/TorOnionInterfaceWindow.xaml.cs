using Modeel.FastTcp;
using Modeel.Messages;
using Modeel.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace Modeel
{
    /// <summary>
    /// Interaction logic for TorOnionInterfaceWindow.xaml
    /// </summary>
    public partial class TorOnionInterfaceWindow : BaseWindowForWPF
    {
        private readonly string _torDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tor");
        private readonly string _defaultTorrcContent = "SocksPort 9050\nControlPort 9051";
        private readonly IPAddress _ipAddress = IPAddress.Loopback;
        private readonly int _sockPort = 9050;
        private readonly int _controlPort = 9051;
        private IUniversalClientSocket _controlSocket;


        public bool TorDirectoryExist => Directory.Exists(_torDirectoryPath);
        public string TorrcFilePath => Path.Combine(_torDirectoryPath, "torrc");
        public bool TorrcFileExist => File.Exists(TorrcFilePath);

        public TorOnionInterfaceWindow()
        {
            InitializeComponent();
            contract.Add(MsgIds.MessageReceiveMessage, typeof(MessageReceiveMessage));
            contract.Add(MsgIds.SocketStateChangeMessage, typeof(SocketStateChangeMessage));

            Init();

            Closed += Window_closedEvent;


            if (!TorDirectoryExist)
            {
                Directory.CreateDirectory(_torDirectoryPath);
            }

            if (!TorrcFileExist)
            {
                FillTorrcWithDefaultContent();
            }

            _controlSocket = new ClientBussinesLogic(_ipAddress, _controlPort, this);
        }

        internal void Init()
        {
            msgSwitch
             .Case(contract.GetContractId(typeof(MessageReceiveMessage)), (MessageReceiveMessage x) => MessageReceiveMessageHandler(x.Message))
             .Case(contract.GetContractId(typeof(SocketStateChangeMessage)), (SocketStateChangeMessage x) => SocketStateChangeMessageHandler(x))
             ;
        }

        private void MessageReceiveMessageHandler(byte[] message)
        {
            string stringMessage = Encoding.ASCII.GetString(message);
            tbkTextForControlSocket.Text += "[SERVER]: " + stringMessage;
            tbkTextForControlSocket.ScrollToEnd();
        }

        private void SocketStateChangeMessageHandler(SocketStateChangeMessage message)
        {
            if (message.SocketState == SocketState.CONNECTED)
            {
                rtgControlSocketState.Fill = new SolidColorBrush(Colors.Green);
                SendStringToControlSocket("AUTHENTICATE ");
                SendStringToControlSocket("SETEVENTS CIRC");

                //SendStringToControlSocket("SETEVENTS CIRC INFO WARN ERR");
                //SendStringToControlSocket("SETEVENTS CIRC STREAM DEBUG INFO NOTICE WARN ERR");

            }
            else if (message.SocketState == SocketState.DISCONNECTED)
            {
                rtgControlSocketState.Fill = new SolidColorBrush(Colors.Red);
            }
        }

        private void FillTorrcWithDefaultContent()
        {
            File.WriteAllText(TorrcFilePath, _defaultTorrcContent);
        }

        private void Window_closedEvent(object? sender, EventArgs e)
        {
            Closed -= Window_closedEvent;

            _controlSocket.DisconnectAndStop();
            _controlSocket.Dispose();
        }

        private void SendStringToControlSocket(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                message += "\r\n";
                _controlSocket.Send(Encoding.ASCII.GetBytes(message));
                tbkTextForControlSocket.Text += "[CLIENT]: " + message;
                tbkTextForControlSocket.ScrollToEnd();  
            }
        }

        private void btSendToControlSocket_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SendStringToControlSocket(tbTextForControlSocket.Text);
        }

        private void btStartTor_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process[] runningProcessByName = Process.GetProcessesByName("tor");
            if (runningProcessByName.Length == 0)
            {
                Process.Start("tor.exe");
            }
        }
    }
}
