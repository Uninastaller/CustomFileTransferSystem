using Common.Enum;
using Common.Interface;
using ConfigManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace SslTcpSession
{
    public static class NodeSynchronization
    {

        private static readonly SslContext _contextForP2pAsClient = new SslContext(SslProtocols.Tls12, Certificats.GetCertificate("MyTestCertificateClient.pfx",
            Certificats.CertificateType.Client), (sender, certificate, chain, sslPolicyErrors) => true);

        private static SemaphoreSlim? _semaphore;

        public static async Task ExecuteSynchronization(IWindowEnqueuer gui, int maximumParallelRunningSockets = 10)
        {
            if (_semaphore != null)
            {
                Logger.Log.WriteLog(Logger.LogLevel.WARNING, "Previous operation not completed yet !");
                return;
            }

            HashSet<Guid> processedNodes; // Sledovanie už spracovaných uzlov
            if (NodeDiscovery.IsSynchronizationOlderThanMaxOldSynchronizationTime())
            {
                processedNodes = new HashSet<Guid>();
                NodeDiscovery.NodeSynchronizationStarted();
            }
            else
            {
                processedNodes = NodeDiscovery.GetAllCurrentlyVerifiedActiveNodeGuids().ToHashSet();
            }

            _semaphore = new SemaphoreSlim(maximumParallelRunningSockets);
            List<Task> tasks = new List<Task>();

            while (true)
            {
                List<Node> nodesToProcess = NodeDiscovery.GetAllNodes().Where(node => !processedNodes.Contains(node.Id)).ToList();

                if (!nodesToProcess.Any())
                {
                    if (_semaphore.CurrentCount < maximumParallelRunningSockets)
                    {
                        await Task.Delay(100); // Čaká na krátky čas pred opätovným skúsením
                    }
                    else
                    {
                        break; // Ak nie sú žiadne nové uzly na spracovanie, ukončí slučku
                    }
                }

                foreach (var node in nodesToProcess)
                {
                    processedNodes.Add(node.Id); // Pridanie uzla do zoznamu spracovaných
                    tasks.Add(SynchronizeNodeAsync(node, gui));
                }

                await Task.WhenAll(tasks); // Čakanie na dokončenie všetkých úloh
                tasks.Clear(); // Vyčistenie zoznamu úloh pre ďalšiu iteráciu
            }
            _semaphore = null;
            NodeDiscovery.NodeSynchronizationFinished();
        }

        private static async Task SynchronizeNodeAsync(Node node, IWindowEnqueuer gui)
        {
            await _semaphore.WaitAsync();

            try
            {
                new SslClientBussinesLogic(_contextForP2pAsClient, IPAddress.Parse(node.Address), node.Port, gui,
                            typeOfSession: TypeOfSession.NODE_DISCOVERY, optionReceiveBufferSize: 0x2000, optionSendBufferSize: 0x2000);
            }
            catch
            {
                _semaphore.Release();
            }
        }

        public static void ReleaseSem()
        {
            _semaphore?.Release();
        }
    }
}
