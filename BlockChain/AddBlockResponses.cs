using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
   public enum AddBlockResponses
   {
      SUCCES = 0,
      INVALID_CREDIT_VALUE_TO_ADD = 1,
      VALIDATION_TEST_FAILD = 2,
      FILE_DOES_NOT_EXIST = 3,
      FILE_IS_FLAGED_TO_BE_REMOVED = 4,
      YOUR_ENDPOINT_IS_NOT_ON_LIST = 5,
      YOUR_ENDPOINT_IS_ALREADY_ON_LIST = 6,

   }
}
