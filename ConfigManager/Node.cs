using System;
using System.Text.Json;

namespace ConfigManager
{
   public class Node
   {
      //public string Id => $"{Address}:{Port}";
      public Guid Id { get; set; }
      public string Address { get; set; } = string.Empty;
      public int Port { get; set; }
      public string PublicKey { get; set; } = string.Empty;

      public string GetJson() => JsonSerializer.Serialize(this);
      public static Node? ToObjectFromJson(string jsonString) => JsonSerializer.Deserialize<Node>(jsonString);

   }
}
