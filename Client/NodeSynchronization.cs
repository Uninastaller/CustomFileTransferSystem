using Common.Enum;
using Common.Interface;
using ConfigManager;
using SslTcpSession;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    static class NodeSynchronization
    {
        static SemaphoreSlim? _semaphore;

        public static async Task ExecuteSynchronization(SslContext context, IWindowEnqueuer gui, int maximumParallelRunningSockets = 10)
        {
            if (_semaphore != null)
            {
                Logger.Log.WriteLog(Logger.LogLevel.WARNING, "Previous operation not completed yet !");
                return;
            }
            _semaphore = new SemaphoreSlim(maximumParallelRunningSockets);
            List<Task> tasks = new List<Task>();
            HashSet<Guid> processedNodes = new HashSet<Guid>(); // Sledovanie už spracovaných uzlov

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
                    tasks.Add(SynchronizeNodeAsync(node, context, gui));
                }

                await Task.WhenAll(tasks); // Čakanie na dokončenie všetkých úloh
                tasks.Clear(); // Vyčistenie zoznamu úloh pre ďalšiu iteráciu
            }
            _semaphore = null;
        }

        private static async Task SynchronizeNodeAsync(Node node, SslContext context, IWindowEnqueuer gui)
        {
            await _semaphore.WaitAsync();

            try
            {
                new SslClientBussinesLogic(context, IPAddress.Parse(node.Address), node.Port, gui,
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
