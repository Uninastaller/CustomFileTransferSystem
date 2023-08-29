using Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Model
{
    public static class FlagMessageEvaluator
    {
        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields



        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor



        #endregion Ctor

        #region PublicMethods

        public static bool EvaluateRequestFile(byte[] buffer, long offset, long size, out string fileName, out Int64 fileSize)
        {
            // Message has 3 parts: FLAG, FILE_NAME, FILE_SIZE
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);
            if (messageParts.Length == 3 && long.TryParse(messageParts[2], out fileSize))
            {
                fileName = messageParts[1];
                return true;
            }
            fileName = string.Empty;
            fileSize = 0;
            return false;
        }

        public static bool EvaluateRequestFilePart(byte[] buffer, long offset, long size, out Int64 filePartNumber, out Int32 partSize)
        {
            // Message has 3 parts: FLAG, FILE_PART_NUMBER, PART_SIZE
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);
            if (messageParts.Length == 3 && long.TryParse(messageParts[1], out filePartNumber) && int.TryParse(messageParts[2], out partSize))
            {
                return true;
            }
            filePartNumber = 0;
            partSize = 0;
            return false;
        }

        #endregion PublicMethods

        #region PrivateMethods



        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region Events



        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods
    }
}
