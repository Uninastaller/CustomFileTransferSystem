using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Frq
{
   public class BaseMsg
   {
      public int ai { get; }

      public BaseMsg(Type type)
      {
         ai = ContractType.GetInstance().GetContractId(type);
      }
   }
}
