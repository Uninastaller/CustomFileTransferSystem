using Modeel.Frq;
using Modeel.Messages;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Modeel
{

    public class ServerBussinesLogic : SslServer
    {
        private readonly IWindowEnqueuer _gui;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Dictionary<Guid, string> _clients = new Dictionary<Guid, string>();

        public ServerBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, int optionAcceptorBacklog = 1024) : base(context, address, port, optionAcceptorBacklog)
        {
            _gui = gui;
            Start();
        }

        protected override SslSession CreateSession() { return new ServerSession(this); }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Tcp server caught an error with code {error}", LoggerInfo.tcpServer);
        }

        private void OnClientDisconnected(SslSession session)
        {
            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage -= OnReceiveMessage;
                serverSession.ClientDisconnected -= OnClientDisconnected;
            }

            ClientStateChange(SocketState.DISCONNECTED, null, session.Id);
            _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients }); 
        }

        protected override void OnConnected(SslSession session)
        {
            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage += OnReceiveMessage;
                serverSession.ClientDisconnected += OnClientDisconnected;
            }

            ClientStateChange(SocketState.CONNECTED, session.Socket?.RemoteEndPoint?.ToString(), session.Id);
            _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
        }

        private void OnReceiveMessage(SslSession sesion, string message)
        {
            Logger.WriteLog($"Tcp server obtained a message: {message}, from: {sesion.Socket.RemoteEndPoint}", LoggerInfo.socketMessage);
            
            _stopwatch.Start();
            SendFile("C:\\Users\\tomas\\Downloads\\The.Office.US.S05.Season.5.Complete.720p.NF.WEB.x264-maximersk [mrsktv]\\The.Office.US.S05E15.720p.NF.WEB.x264-MRSK.mkv", sesion);
            _stopwatch.Stop();
            TimeSpan elapsedTime = _stopwatch.Elapsed;
            Logger.WriteLog($"File transfer completed in {elapsedTime.TotalSeconds} seconds.", LoggerInfo.P2PSSL); ;
        }

        private void ClientStateChange(SocketState socketState, string? client, Guid sessionId)
        {
            if (socketState == SocketState.CONNECTED && !_clients.ContainsKey(sessionId) && client != null)
            {
                _clients.Add(sessionId, client);
                Logger.WriteLog($"Client: {client}, connected to server ", LoggerInfo.tcpServer);
            }
            else if (socketState == SocketState.DISCONNECTED && _clients.ContainsKey(sessionId))
            {
                Logger.WriteLog($"Client: {_clients[sessionId]}, disconnected from server ", LoggerInfo.tcpServer);
                _clients.Remove(sessionId);
            }
        }

        private void SendFile(string filePath, SslSession session)
        {            
            // Open the file for reading
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Choose an appropriate buffer size based on the file size and system resources
                int bufferSize = CalculateBufferSize(fileStream.Length);
                Logger.WriteLog($"Fille buffer choosed for: {bufferSize}", LoggerInfo.P2PSSL);

                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Send the bytes read from the file over the network stream
                    //SslSession session = FindSession(_clients.ElementAt(0).Key);
                    session.Send(buffer, 0, bytesRead);
                }
            }
        }

        private int CalculateBufferSize(long fileSize)
        {
            // Determine the available system memory
            long availableMemory = GC.GetTotalMemory(false);

            // Choose a buffer size based on the file size and available memory
            if (fileSize <= availableMemory)
            {
                // If the file size is smaller than available memory, use a buffer size equal to the file size
                return (int)fileSize;
            }
            else
            {
                // Otherwise, choose a buffer size that is a fraction of available memory
                double bufferFraction = 0.1;
                int bufferSize = (int)(availableMemory * bufferFraction);

                // Ensure the buffer size is at least 4KB and at most 1MB
                return Math.Max(4096, Math.Min(bufferSize, 1048576));
            }
        }
    }
}
