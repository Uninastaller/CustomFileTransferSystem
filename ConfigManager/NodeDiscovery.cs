using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ConfigManager
{
    static public class NodeDiscovery
    {
        private static ConcurrentDictionary<Guid, Node> _nodes = new ConcurrentDictionary<Guid, Node>();
        private static ConcurrentDictionary<Guid, Node> _currentlyVerifiedActiveNodes = new ConcurrentDictionary<Guid, Node>();
        private static Node _myNode = new Node();

        public static IPAddress LocalIpAddress { get; private set; } = IPAddress.None;
        public static IPAddress PublicIpAddress { get; private set; } = IPAddress.None;

        public static DateTime LastTimeOfSynchronization { get; private set; } = DateTime.MinValue;
        public static TimeSpan PassedTimeFromLastSynchronization => DateTime.UtcNow - LastTimeOfSynchronization;
        public static string SynchronizationHash { get; private set; } = string.Empty;

        public static readonly int MaxOldSynchronization = 60;

        public static void StartApplication()
        {
            LoadMyNode();
            LoadNodes();
        }

        public static void SetIpAddresses(IPAddress localIpAddress, IPAddress publicIpAddress)
        {
            LocalIpAddress = localIpAddress;
            PublicIpAddress = publicIpAddress;
        }

        public static void LoadNodes()
        {
            string nodesFilePath = MyConfigManager.GetConfigStringValue("StoredNodesFilePath");
            if (File.Exists(nodesFilePath))
            {
                try
                {
                    Dictionary<Guid, Node>? nodes = JsonSerializer.Deserialize<Dictionary<Guid, Node>>(File.ReadAllText(nodesFilePath));
                    if (nodes != null)
                    {
                        _nodes = new ConcurrentDictionary<Guid, Node>(nodes);
                        return;
                    }
                }
                catch
                {
                    _nodes = new ConcurrentDictionary<Guid, Node>();
                }
                finally
                {
                    AddNode(_myNode);
                }
            }
            else
            {
                _nodes = new ConcurrentDictionary<Guid, Node>();
                AddNode(_myNode);
            }
        }

        public static void LoadMyNode()
        {
            string nodesFilePath = MyConfigManager.GetConfigStringValue("MyNodeInfo");
            if (File.Exists(nodesFilePath))
            {
                try
                {
                    Node? node = JsonSerializer.Deserialize<Node>(File.ReadAllText(nodesFilePath));
                    if (node != null)
                    {
                        _myNode = node;
                    }
                }
                catch
                { }
            }
            else if (CreateNewNodeForMe(out Node node))
            {
                _myNode = node;
                SaveMyNode();
            }
        }

        private static bool CreateNewNodeForMe(out Node node)
        {
            node = new Node();
            if (!MyConfigManager.TryGetIntConfigValue("UploadingServerPort", out Int32 port) || LocalIpAddress == IPAddress.None)
            {
                return false;
            }

            node.Id = Guid.NewGuid();
            node.Port = port;
            node.Address = LocalIpAddress.ToString();
            node.PublicKey = Certificats.ExportPublicKeyToJSON(Certificats.GetCertificate("NodeXY", Certificats.CertificateType.Node));
            return true;
        }

        public static bool SaveMyNode()
        {
            try
            {
                File.WriteAllText(MyConfigManager.GetConfigStringValue("MyNodeInfo"), JsonSerializer.Serialize(_myNode));
                return true;
            }
            catch
            {
                // Handle or log exception here
                return false;
            }
        }

        public static bool UpdateAndSaveMyNode(Node node)
        {
            _myNode.Id = node.Id;
            _myNode.Address = node.Address;
            _myNode.Port = node.Port;
            _myNode.PublicKey = node.PublicKey;
            return SaveMyNode();
        }

        public static string LoadNodesAsString()
        {
            string nodesFilePath = MyConfigManager.GetConfigStringValue("StoredNodesFilePath");
            if (File.Exists(nodesFilePath))
            {
                return File.ReadAllText(nodesFilePath);
            }
            return string.Empty;
        }

        public static bool SaveNodes()
        {
            try
            {
                File.WriteAllText(MyConfigManager.GetConfigStringValue("StoredNodesFilePath"), JsonSerializer.Serialize(_nodes));
                return true;
            }
            catch
            {
                // Handle or log exception here
                return false;
            }
        }

        public static void AddNode(Node node)
        {
            // Check is you did add your custom node and if its connected
            if (_nodes.Any(pair => pair.Key == Guid.Empty && pair.Value.Port == node.Port && pair.Value.Address.Equals(node.Address)))
            {
                _nodes.Remove(Guid.Empty, out _);
            }

            if (!_nodes.TryAdd(node.Id, node))
            {
                //_nodes[node.Id].PublicKey = node.PublicKey;
                _nodes[node.Id].Port = node.Port;
                _nodes[node.Id].Address = node.Address;
            }          
        }

        public static bool RemoveNode(Guid nodeId)
        {
            return _nodes.TryRemove(nodeId, out _);
        }

        public static bool TryGetNode(Guid nodeId, [MaybeNullWhen(false)] out Node node)
        {
            return _nodes.TryGetValue(nodeId, out node);
        }

        public static IEnumerable<Node> GetAllNodes()
        {
            return _nodes.Values;
        }

        public static IEnumerable<Node> GetAllCurrentlyVerifiedActiveNodes()
        {
            return _currentlyVerifiedActiveNodes.Values;
        }

        public static IEnumerable<Guid> GetAllCurrentlyVerifiedActiveNodeGuids()
        {
            return _currentlyVerifiedActiveNodes.Keys;
        }

        public static void UpdateNodeList(Dictionary<Guid, Node> externalNodes)
        {
            foreach (var node in externalNodes.Values)
            {
                AddNode(node);
            }
        }

        public static void UpdateCurrentlyVerifiedActiveNodeList(Node verifiedActiveNode)
        {
            if (!_currentlyVerifiedActiveNodes.TryAdd(verifiedActiveNode.Id, verifiedActiveNode))
            {
                //_currentlyVerifiedActiveNodes[verifiedActiveNode.Id].PublicKey = verifiedActiveNode.PublicKey;
                _currentlyVerifiedActiveNodes[verifiedActiveNode.Id].Port = verifiedActiveNode.Port;
                _currentlyVerifiedActiveNodes[verifiedActiveNode.Id].Address = verifiedActiveNode.Address;
            }
        }

        public static void UpdateCurrentlyVerifiedActiveNodeList(string address, int port)
        {
            Node node = _nodes.FirstOrDefault(pair => pair.Value.Address.Equals(address) && pair.Value.Port == port).Value;
            if (node != null)
            {
                UpdateCurrentlyVerifiedActiveNodeList(node);
            }
        }

        public static Node GetMyNode() => _myNode;

        private static void ClearCurrentlyVerifiedActiveNodes()
        {
            _currentlyVerifiedActiveNodes.Clear();
        }

        public static IEnumerable<NodeForGrid> GetNodesForGrid()
        {
            return _nodes.Select(n => new NodeForGrid(n.Value)
            {
                Active = _currentlyVerifiedActiveNodes.ContainsKey(n.Key)
            });
        }

        private static string GetHashFromActiveNodes()
        {
            IEnumerable<string> sortedGuids = _currentlyVerifiedActiveNodes.Keys.OrderBy(guid => guid).Select(guid => guid.ToString());

            StringBuilder sb = new StringBuilder();
            foreach (string guid in sortedGuids)
            {
                sb.Append(guid);
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
            }
        }


        public static void NodeSynchronizationStarted()
        {
            ClearCurrentlyVerifiedActiveNodes();
        }

        public static void NodeSynchronizationFinished()
        {
            LastTimeOfSynchronization = DateTime.UtcNow;
            SynchronizationHash = GetHashFromActiveNodes();
        }

        public static bool IsSynchronizationOlderThanMaxOldSynchronizationTime()
        {
            return PassedTimeFromLastSynchronization.TotalSeconds > MaxOldSynchronization;
        }
    }
}
