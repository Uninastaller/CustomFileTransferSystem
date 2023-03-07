using Modeel.SSL;
using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Threading;

namespace Modeel
{
    /// <summary>
    /// Interaction logic for TestWindow2.xaml
    /// </summary>
    public partial class TestWindow2 : BaseWindowForWPF
    {
        private ClientBussinesLogic _clientBussinesLogic;

        private readonly int _serverPort = 8080;
        private readonly IPAddress _ipAddress = IPAddress.Loopback;
        private readonly string _certificateName = "MyTestCertificateClient.pfx";

        public TestWindow2()
        {
            InitializeComponent();
            contract.Add(MsgIds.WindowStateSetMessage, typeof(WindowStateSetMessage));

            Init();

            Closed += Window_closedEvent;

            SslContext context = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), ""), (sender, certificate, chain, sslPolicyErrors) => true);
            _clientBussinesLogic = new ClientBussinesLogic(context, _ipAddress, _serverPort);

        }
        internal void Init()
        {
            msgSwitch
             .Case(contract.GetContractId(typeof(WindowStateSetMessage)), (WindowStateSetMessage x) => WindowStateSetMessageHandler(x));
        }

        private void WindowStateSetMessageHandler(WindowStateSetMessage message)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                WindowState = System.Windows.WindowState.Normal;
            }
            Activate();
        }

        private void Window_closedEvent(object? sender, EventArgs e)
        {
            Closed -= Window_closedEvent;
            _clientBussinesLogic.DisconnectAndStop();
            _clientBussinesLogic.Dispose();
        }
    }
}