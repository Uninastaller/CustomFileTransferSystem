using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ConfigManager
{
   public class Node
   {
      public Guid Id { get; set; }
      public string Address { get; set; } = string.Empty;
      public int Port { get; set; }
      public string PublicKey { get; set; } = string.Empty;

      public string GetJson() => JsonSerializer.Serialize(this);
      public static Node? ToObjectFromJson(string jsonString) => JsonSerializer.Deserialize<Node>(jsonString);
      public EndPoint? GetNodeEndpoint()
      {

         if (!IPAddress.TryParse(Address, out IPAddress? address))
         {
            return null;
         }

         return new IPEndPoint(address, Port);
      }
      public bool TryGetNodeCustomEndpoint([MaybeNullWhen(false)] out IpAndPortEndPoint endPoint)
      {
         if (!IPAddress.TryParse(Address, out IPAddress? address))
         {
            endPoint = null;
            return false;
         }
         endPoint = new IpAndPortEndPoint() { Port = Port, IpAddress = address.ToString() };
         return true;
      }
      public string GenerateNodeSpecificHash(string lastBlockHash, DateTime requestedBlockTimestamp)
      {
         using (SHA256 sha256 = SHA256.Create())
         {
            string input = lastBlockHash + Id.ToString() + requestedBlockTimestamp.ToString("HH:mm:ss:fff");
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
         }
      }

      public Node(NodeForGrid node)
      {
         Id = node.Id;
         Port = node.Port;
         PublicKey = node.PublicKey;
         Address = node.Address;
      }

      public Node()
      {

      }
   }
}
