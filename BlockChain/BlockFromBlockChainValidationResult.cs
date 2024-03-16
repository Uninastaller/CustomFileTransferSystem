using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
   public enum BlockFromBlockChainValidationResult
   {
      VALID = 0,
      INVALID_PREVIOUS_HASH = 1,
      INVALID_SIGN = 2,
      UNABLE_TO_DECIDE = 3,
      INVALID_CREDIT_CALCULATION = 4,
      INVALID_PRICE_CALCULATION = 5,
      INVALID_BLOCK_INDEX = 6,
   }
}
