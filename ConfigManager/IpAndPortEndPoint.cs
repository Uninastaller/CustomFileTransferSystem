namespace ConfigManager
{
   public class IpAndPortEndPoint
   {
      public string IpAddress { get; set; } = string.Empty;
      public int Port { get; set; }

      public override string ToString()
      {
         return IpAddress + ":" + Port;
      }
   }
}
