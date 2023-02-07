using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Frq
{
   public interface IContractType
   {
      Type GetContractType(int contractTypeId);

      int GetContractId(Type contractType);

      void Add(int msgId, Type t);
   }
}
