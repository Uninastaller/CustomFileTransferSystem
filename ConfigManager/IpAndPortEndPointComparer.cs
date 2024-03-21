using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigManager
{
   public class IpAndPortEndPointComparer : EqualityComparer<IpAndPortEndPoint>
   {
      public override bool Equals(IpAndPortEndPoint? x, IpAndPortEndPoint? y)
      {
         // Kontrola, či sú obidve referencie nulové alebo rovnaké
         if (ReferenceEquals(x, y)) return true;

         // Kontrola, či je niektorá z referencií nulová
         if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
            return false;

         // Porovnanie hodnôt
         return x.IpAddress == y.IpAddress && x.Port == y.Port;
      }

      public override int GetHashCode(IpAndPortEndPoint obj)
      {
         // Výpočet hash kódu
         int hashIp = obj.IpAddress == null ? 0 : obj.IpAddress.GetHashCode();
         int hashPort = obj.Port.GetHashCode();

         return hashIp ^ hashPort; // XOR operácia pre kombináciu hash kódov
      }
   }
}
