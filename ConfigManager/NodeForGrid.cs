using System;

namespace ConfigManager
{
   public class NodeForGrid
   {
      public Guid Id { get; private set; }
      public string Address { get; private set; } = string.Empty;
      public int Port { get; private set; }
      public string PublicKey { get; private set; } = string.Empty;
      public bool Active { get; set; }

      public NodeForGrid(Node node)
      {
         Id = node.Id;
         Address = node.Address;
         Port = node.Port;
         PublicKey = node.PublicKey;
      }
   }
}
