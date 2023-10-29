using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;

namespace ConfigManager
{
    static public class NodeDiscovery
    {
        private static ConcurrentDictionary<string, Node> _nodes = new ConcurrentDictionary<string, Node>();

        static NodeDiscovery()
        {
            LoadNodes();
        }

        public static void LoadNodes()
        {
            string nodesFilePath = MyConfigManager.GetConfigValue("StoredNodesFilePath");
            if (File.Exists(nodesFilePath))
            {
                try
                {
                    var nodes = JsonSerializer.Deserialize<Dictionary<string, Node>>(File.ReadAllText(nodesFilePath));
                    if (nodes != null)
                    {
                        _nodes =  new ConcurrentDictionary<string, Node>(nodes);
                        return;
                    }
                }
                catch
                {
                    _nodes = new ConcurrentDictionary<string, Node>();
                }
            }
            _nodes = new ConcurrentDictionary<string, Node>();
        }

        public static bool SaveNodes()
        {
            try
            {
                File.WriteAllText(MyConfigManager.GetConfigValue("StoredNodesFilePath"), JsonSerializer.Serialize(_nodes));
                return true;
            }
            catch
            {
                // Handle or log exception here
                return false;
            }
        }

        public static bool AddNode(Node node)
        {
            return _nodes.TryAdd(node.Id, node);
        }

        public static bool RemoveNode(string nodeId)
        {
            return _nodes.TryRemove(nodeId, out _);
        }

        public static bool GetNode(string nodeId, [MaybeNullWhen(false)] out Node node)
        {
            return _nodes.TryGetValue(nodeId, out node);
        }

        public static IEnumerable<Node> GetAllNodes()
        {
            return _nodes.Values;
        }

        public static void UpdateNodeList(Dictionary<string, Node> externalNodes)
        {
            foreach(var node in externalNodes.Values)
            {
                if (!AddNode(node))
                {
                    _nodes[node.Id].PublicKey = node.PublicKey;
                }
            }
        }

    }
}
