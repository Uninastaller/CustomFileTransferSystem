using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
   public enum BlockValidationResult
   {
      VALID = 0,
      INVALID_PREVIOUS_HASH = 1,
      INVALID_SIGN = 2,
      UNABLE_TO_DECIDE = 3,
      INVALID_CREDIT_CALCULATION = 4,
      INVALID_PRICE_CALCULATION = 5,
      INVALID_BLOCK_INDEX = 6,
      INVALID_CREDIT_VALUE_TO_ADD = 7,
      VALIDATION_TEST_FAILD = 8,
      FILE_DOES_NOT_EXIST = 9,
      FILE_IS_FLAGED_TO_BE_REMOVED = 10,
      YOUR_ENDPOINT_IS_NOT_ON_LIST = 11,
      YOUR_ENDPOINT_IS_ALREADY_ON_LIST = 12,
      NOT_ENOUGHT_CREDIT = 13,
      UNDEFINED_SITUATION = 14,
      NEGATIVE_VALUE_OF_CREDIT = 15,
      INVALID_FILE_LOCATIONS = 16,
      INVALID_NODE_ENDPOINT = 17
   }
}
