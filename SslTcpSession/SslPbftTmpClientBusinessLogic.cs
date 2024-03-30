using Common.Enum;
using Common.Interface;
using Common.Model;
using ConfigManager;
using Logger;
using SslTcpSession.BlockChain;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
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

        private bool _isDisposing = false;
        private static readonly SslContext _replicaContext = new SslContext(SslProtocols.Tls12, Certificats.GetCertificate("ReplicaXY",
            Certificats.CertificateType.Node), (sender, certificate, chain, sslPolicyErrors) => true);

        public delegate void ReceivePbftMessageEventHandler(PbftReplicaLogDto log);
        public static event ReceivePbftMessageEventHandler? ReceivePbftMessage;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        public SslPbftTmpClientBusinessLogic(IPAddress ipAddress, int port)
        : base(_replicaContext, ipAddress, port, _receiveBufferSize, _sendBufferSize)
        {

        }

        #endregion Ctor

        #region PublicMethods

        private static void OnReceivePbftMessage(PbftReplicaLogDto log)
        {
            ReceivePbftMessage?.Invoke(log);
        }

        public static async Task<bool> SendPbftRequestAndDispose(IPAddress ipAddress, int port, Block requestedBlock, string synchronizationHash)
        {
            Log.WriteLog(LogLevel.INFO, $"Sending request to primary replica: {ipAddress}:{port}, with synchronization hash: {synchronizationHash}");

            bool returnValue = false;

            await Task.Run(() =>
            {
                SslPbftTmpClientBusinessLogic bs = new SslPbftTmpClientBusinessLogic(ipAddress, port);
                if (bs.Connect())
                {
                    MethodResult result = FlagMessagesGenerator.GeneratePbftRequest(bs, requestedBlock.ToJson(), synchronizationHash);
                    if (result == MethodResult.ERROR)
                    {
                        Log.WriteLog(LogLevel.ERROR, $"Error sending request message to {ipAddress}:{port}");
                    }
                    else
                    {
                        Log.WriteLog(LogLevel.INFO, $"Request message successfully sent to {ipAddress}:{port}");
                        returnValue = true;
                    }
                }
                else
                {
                    Log.WriteLog(LogLevel.INFO, $"Unable to connect to {ipAddress}:{port}");
                }

                bs.StopAndDispose();
            });
            return returnValue;
        }


        public static async Task MulticastPrePrepare(Block requestedBlock, Guid primaryReplicaId,
            string signOfPrimaryReplica, string synchronizationHash)
        {
            Log.WriteLog(LogLevel.INFO, $"Sending pre-prepare with multicast to all replicas, with synchronization hash: {synchronizationHash}");

            int maxConcurrentTasks = 10;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);

            List<Task> tasks = new List<Task>();
            foreach (Node node in NodeDiscovery.GetAllCurrentlyVerifiedActiveNodes())
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    if (IPAddress.TryParse(node.Address, out IPAddress? address))
                    {
                        SslPbftTmpClientBusinessLogic bs = new SslPbftTmpClientBusinessLogic(address, node.Port);
                        if (bs.Connect())
                        {
                            MethodResult result = FlagMessagesGenerator.GeneratePbftPrePrepare(bs, requestedBlock.ToJson(),
                                primaryReplicaId.ToString(), signOfPrimaryReplica, synchronizationHash);

                            if (result == MethodResult.ERROR)
                            {
                                Log.WriteLog(LogLevel.ERROR, $"Error sending pre-prepare message to {address}:{node.Port}");
                            }
                            else
                            {
                                OnReceivePbftMessage(new PbftReplicaLogDto(SocketMessageFlag.PBFT_PRE_PREPARE, MessageDirection.SENT,
                                    synchronizationHash, requestedBlock.Hash, node.Id.ToString(), primaryReplicaId.ToString(), DateTime.UtcNow));

                                Log.WriteLog(LogLevel.INFO, $"Pre-prepare message successfully sent to {address}:{node.Port}");
                            }
                        }
                        else
                        {
                            Log.WriteLog(LogLevel.INFO, $"Unable to connect to {address}:{node.Port}");
                        }

                        bs.StopAndDispose();
                    }

                    semaphore.Release();
                }));
            }

            await Task.WhenAll(tasks);
        }

        public static async Task MulticastPrepare(string hashOfRequest, string signOfBackupReplica,
            string synchronizationHash, Guid guidOfBackupReplica)
        {
            Log.WriteLog(LogLevel.INFO, $"Sending prepare with multicast to all replicas, with synchronization hash: {synchronizationHash}");

            int maxConcurrentTasks = 10;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);

            List<Task> tasks = new List<Task>();
            foreach (Node node in NodeDiscovery.GetAllCurrentlyVerifiedActiveNodes())
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    if (IPAddress.TryParse(node.Address, out IPAddress? address))
                    {
                        SslPbftTmpClientBusinessLogic bs = new SslPbftTmpClientBusinessLogic(address, node.Port);
                        if (bs.Connect())
                        {
                            MethodResult result = FlagMessagesGenerator.GeneratePbftPrepare(bs, hashOfRequest,
                                signOfBackupReplica, synchronizationHash, guidOfBackupReplica.ToString());

                            if (result == MethodResult.ERROR)
                            {
                                Log.WriteLog(LogLevel.ERROR, $"Error sending prepare message to {address}:{node.Port}");
                            }
                            else
                            {
                                OnReceivePbftMessage(new PbftReplicaLogDto(SocketMessageFlag.PBFT_PREPARE, MessageDirection.SENT,
                                    synchronizationHash, hashOfRequest, node.Id.ToString(), guidOfBackupReplica.ToString(), DateTime.UtcNow));

                                Log.WriteLog(LogLevel.INFO, $"Prepare message successfully sent to {address}:{node.Port}");
                            }
                        }
                        else
                        {
                            Log.WriteLog(LogLevel.INFO, $"Unable to connect to {address}:{node.Port}");
                        }

                        bs.StopAndDispose();
                    }

                    semaphore.Release();
                }));
            }

            await Task.WhenAll(tasks);
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

        #endregion Events

        #region OverridedMethods

        protected override void Dispose(bool disposingManagedResources)
        {
            Log.WriteLog(LogLevel.DEBUG, $"Ssl pbft tmp client with Id {Id} is being disposed");

            base.Dispose(disposingManagedResources);
        }

        protected override void OnDisconnected()
        {
            Log.WriteLog(LogLevel.DEBUG, $"Ssl pbft tmp client disconnected from session with Id: {Id}");
        }

        #endregion OverridedMethods
    }
}
