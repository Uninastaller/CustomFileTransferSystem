using Logger;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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

        public static bool EvaluateOfferingFile(byte[] buffer, long offset, long size, [MaybeNullWhen(false)] out OfferingFileDto? offeringFileDto)
        {
            // Message has 2 parts: FLAG, OFFERING_FILE_ON_JSON_FORMAT
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);
            if (messageParts.Length == 2)
            {
                try
                {
                    offeringFileDto = JsonConvert.DeserializeObject<OfferingFileDto>(messageParts[1]);
                    Log.WriteLog(LogLevel.INFO, $"Offering file with content: {messageParts[1].Replace('\n', ' ').Replace('\r', ' ')} received and validated!");
                    return true;
                }
                catch (JsonException ex)
                {
                    Log.WriteLog(LogLevel.WARNING, $"Offering file with content: {messageParts[1].Replace('\n', ' ').Replace('\r', ' ')} received and but not valid! " + ex.Message);
                }
            }
            offeringFileDto = null;
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
