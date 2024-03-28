using Common.Enum;
using Common.Interface;
using Common.Model;
using ConfigManager;
using Logger;
using SslTcpSession.BlockChain;
using System;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SslTcpSession
{
   public class SslPbftTmpClientBusinessLogic : SslClient, ISession
   {
      #region Properties

      #endregion Properties

      #region PublicFields

      #endregion PublicFields

      #region PrivateFields

      private static readonly int _receiveBufferSize = 0x800; // 2048b
      private static readonly int _sendBufferSize = 0x2000; // 8192b
      private static readonly Int16 _maxDisconnectTime = 10; // sec

      private Timer? _timer;
      private Int16 _disconnectTime = 0;
      private bool _isDisposing = false;
      private static readonly SslContext _replicaContext = new SslContext(SslProtocols.Tls12, Certificats.GetCertificate("ReplicaXY",
          Certificats.CertificateType.Node), (sender, certificate, chain, sslPolicyErrors) => true);

      #endregion PrivateFields

      #region ProtectedFields



      #endregion ProtectedFields

      #region Ctor

      public SslPbftTmpClientBusinessLogic(IPAddress ipAddress, int port)
      : base(_replicaContext, ipAddress, port, _receiveBufferSize, _sendBufferSize)
      {
         ConnectAsync();

         _timer = new Timer(1000); // Set the interval to 1 second
         _timer.Elapsed += OneSecondHandler;
         _timer.Start();

      }

      #endregion Ctor

      #region PublicMethods

      public static async Task<bool> SendPbftRequestAndDispose(IPAddress ipAddress, int port, Block requestedBlock, string activeReplicasAsHash)
      {
         Log.WriteLog(LogLevel.INFO, $"Sendong request to primary replica: {ipAddress}:{port}, with active replicas has: {activeReplicasAsHash}");
         SslPbftTmpClientBusinessLogic bs = new SslPbftTmpClientBusinessLogic(ipAddress, port);

         while (!bs.IsConnected && !bs.IsDisposed)
            await Task.Delay(200);

         MethodResult result = FlagMessagesGenerator.GeneratePbftRequest(bs, requestedBlock.ToJson(), activeReplicasAsHash);

         if (result == MethodResult.ERROR)
         {
            return false;
         }

         bs.StopAndDispose();
         return true;
      }


      public static async void MulticastPrePrepare(Block requestedBlock, string signOfPrimaryReplica)
      {
         
      }

      #endregion PublicMethods

      #region PrivateMethods

      private void StopAndDispose()
      {
         if (_isDisposing)
         {
            return;
         }

         _isDisposing = true;
         Dispose();
      }

      #endregion PrivateMethods

      #region ProtectedMethods



      #endregion ProtectedMethods

      #region Events

      private void OneSecondHandler(object? sender, ElapsedEventArgs e)
      {
         if (!IsConnected && ++_disconnectTime == _maxDisconnectTime)
         {
            Log.WriteLog(LogLevel.WARNING, $"Unable to connect to the server: {this.Endpoint}. Disposing socked!");
            StopAndDispose();
         }
      }

      #endregion Events

      #region OverridedMethods

      protected override void Dispose(bool disposingManagedResources)
      {
         Log.WriteLog(LogLevel.DEBUG, $"Ssl pbft tmp client with Id {Id} is being disposed");

         if (_timer != null)
         {
            _timer.Elapsed -= OneSecondHandler;
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
         }

         base.Dispose(disposingManagedResources);
      }

      protected override void OnDisconnected()
      {
         Log.WriteLog(LogLevel.DEBUG, $"Ssl pbft tmp client disconnected from session with Id: {Id}");
         // Wait for a while...
         Thread.Sleep(1000);

         // Try to connect again
         if (!_isDisposing)
            ConnectAsync();
      }

      #endregion OverridedMethods
   }
}
