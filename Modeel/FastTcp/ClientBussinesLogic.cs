using Modeel.Frq;
using Modeel.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Modeel.FastTcp
{
    internal class ClientBussinesLogic : IUniversalClientSocket, IDisposable
    {
        public TypeOfSocket Type { get; }
        public string Address => _address.ToString();
        public int Port => _port;
        public Guid Id { get; }
        public bool IsConnecting { get; private set; } = false;
        public bool IsConnected { get; private set; } = false;
        public bool IsDisposed { get; private set; } = false;
        public bool AutoConnect { get; set; } = true;

        public TcpClient? Socket { get; private set; }


        private readonly IWindowEnqueuer _gui;
        private readonly IPAddress _address;
        private readonly int _port;
        private NetworkStream? _stream;
        private IAsyncResult? result;

        private readonly Timer _timer;
        private UInt64 _timerCounter;

        public ClientBussinesLogic(IPAddress address, int port, IWindowEnqueuer gui)
        {
            Id = Guid.NewGuid();
            this.Type = TypeOfSocket.TCP;

            _gui = gui;
            _address = address;
            _port = port;

            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += OneSecondHandler;
            _timer.Start();
        }

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;
            if (_timerCounter%10 == 0)
            {
                if (AutoConnect)
                {
                    ConnectAsync();
                }
            }
        }

        public bool ConnectAsync()
        {

            if (IsConnecting || IsConnected)
            {
                return false;
            }

            try
            {
                AutoConnect = false;

                // Set IsConnecting to true to indicate that the client is attempting to connect
                IsConnecting = true;

                Socket = new TcpClient();

                // Start connecting to the server asynchronously
                result = Socket.BeginConnect(_address, _port, null, null);

                // Wait up to 5 seconds for the connection to complete
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                if (success)
                {
                    // End the asynchronous connection attempt and get a NetworkStream object from the TcpClient
                    Socket.EndConnect(result);
                    _stream = Socket.GetStream();

                    // Set IsConnecting to false to indicate that the client is connected
                    IsConnecting = false;
                    IsConnected = true;
                    return true;
                }
                else
                {
                    Console.WriteLine("Connection attempt timed out.");
                    AutoConnect = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to server: {ex.Message}");

                // Set IsConnecting to false to indicate that the client is not connecting
                IsConnecting = false;

                AutoConnect = true;
                return false;
            }
        }

        public virtual bool DisconnectAsync() => Disconnect();

        public bool Disconnect()
        {

            if (!IsConnected && !IsConnecting)
                return false;

            // Cancel connecting operation
            if (IsConnecting && result != null)
                Socket?.EndConnect(result);

            // Reset connecting flag
            IsConnecting = false;

            try
            {
                // Close the network stream and TcpClient
                _stream?.Close();
                _stream?.Dispose();
                _stream = null;
                Socket?.Close();
                Socket?.Dispose();
                Socket = null;
                IsConnected = false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to disconnect from server: {ex.Message}");
                return false;
            }
        }

        public void DisconnectAndStop()
        {
            AutoConnect = false;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        public void Dispose()
        {
            _timer.Elapsed -= OneSecondHandler;
            _timer.Stop();
            _timer.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposingManagedResources)
        {
            // The idea here is that Dispose(Boolean) knows whether it is
            // being called to do explicit cleanup (the Boolean is true)
            // versus being called due to a garbage collection (the Boolean
            // is false). This distinction is useful because, when being
            // disposed explicitly, the Dispose(Boolean) method can safely
            // execute code using reference type fields that refer to other
            // objects knowing for sure that these other objects have not been
            // finalized or disposed of yet. When the Boolean is false,
            // the Dispose(Boolean) method should not execute code that
            // refer to reference type fields because those objects may
            // have already been finalized."

            Socket?.Close();
            Socket = null;
            _stream?.Dispose();
            _stream = null;

            if (!IsDisposed)
            {
                if (disposingManagedResources)
                {
                    // Dispose managed resources here...
                    DisconnectAsync();
                }

                // Dispose unmanaged resources here...

                // Set large fields to null here...

                // Mark as disposed.
                IsDisposed = true;
            }
        }
    }
}
