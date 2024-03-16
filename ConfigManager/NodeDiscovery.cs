using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;

namespace ConfigManager
{
   static public class NodeDiscovery
   {
      private static ConcurrentDictionary<Guid, Node> _nodes = new ConcurrentDictionary<Guid, Node>();
      private static Node _myNode = new Node();

      public static IPAddress LocalIpAddress { get; private set; } = IPAddress.None;
      public static IPAddress PublicIpAddress { get; private set; } = IPAddress.None;

      //static NodeDiscovery()
      //{
      //   LoadNodes();
      //   LoadMyNode();
      //}

      public static void StartApplication()
      {
         LoadNodes();
         LoadMyNode();
      }

      public static void SetIpAdresses(IPAddress localIpAddress, IPAddress publicIpAddress)
      {
         LocalIpAddress = localIpAddress;
         PublicIpAddress = publicIpAddress;
      }

      public static void LoadNodes()
      {
         string nodesFilePath = MyConfigManager.GetConfigValue("StoredNodesFilePath");
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
         }
         _nodes = new ConcurrentDictionary<Guid, Node>();
      }

      public static void LoadMyNode()
      {
         string nodesFilePath = MyConfigManager.GetConfigValue("MyNodeInfo");
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
         else if(CreateNewNodeForMe(out Node node))
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
            File.WriteAllText(MyConfigManager.GetConfigValue("MyNodeInfo"), JsonSerializer.Serialize(_myNode));
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
         string nodesFilePath = MyConfigManager.GetConfigValue("StoredNodesFilePath");
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
            File.WriteAllText(MyConfigManager.GetConfigValue("StoredNodesFilePath"), JsonSerializer.Serialize(_nodes));
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

      public static bool GetNode(Guid nodeId, [MaybeNullWhen(false)] out Node node)
      {
         return _nodes.TryGetValue(nodeId, out node);
      }

      public static IEnumerable<Node> GetAllNodes()
      {
         return _nodes.Values;
      }

      public static void UpdateNodeList(Dictionary<Guid, Node> externalNodes)
      {
         foreach (var node in externalNodes.Values)
         {
            AddNode(node);
         }
      }

      //public static Node GetMyNode(IPEndPoint endpoint) => GetMyNode(endpoint.Address.ToString(), endpoint.Port);

      //public static Node GetMyNode(string address, int port)
      //{
      //   return new Node() { Address = address, Port = port, PublicKey = Certificats.ExportPublicKeyToJSON(Certificats.GetCertificate("NodeXY", Certificats.CertificateType.Node)) };
      //}
      public static Node GetMyNode() => _myNode;

   }
}
