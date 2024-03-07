using Common.Enum;
using ConfigManager;
using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

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

      public static bool EvaluateRequestFileMessage(byte[] buffer, long offset, long size, out string fileName, out Int64 fileSize)
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

      public static bool EvaluateRequestFilePartMessage(byte[] buffer, long offset, long size, out Int64 filePartNumber, out Int32 partSize)
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

      public static bool EvaluateOfferingFileMessage(byte[] buffer, long offset, long size, out List<OfferingFileDto> offeringFilesDto, out bool endOdMessageGroup)
      {

         offeringFilesDto = new List<OfferingFileDto>();
         bool succes = false;
         endOdMessageGroup = false;

         // Message has 2 parts: FLAG, OFFERING_FILE_ON_JSON_FORMAT
         string messageBlock = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

         string[] messages = messageBlock.Split(new string[] { SocketMessageFlag.END_OF_MESSAGE.GetStringValue() }, StringSplitOptions.None);

         foreach (string message in messages)
         {
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

            if (messageParts.Length == 3)
            {
               try
               {
                  OfferingFileDto? offeringFileDto = OfferingFileDto.ToObjectFromJson(messageParts[1]);
                  if (offeringFileDto != null)
                  {
                     offeringFilesDto.Add(offeringFileDto);
                     Log.WriteLog(LogLevel.INFO, $"Offering file with content: {messageParts[1]} received and validated!");
                  }

                  if (messageParts[2].Equals(SocketMessageFlag.END_OF_MESSAGE_GROUP.GetStringValue()))
                  {
                     endOdMessageGroup = true;
                  }

                  succes = true;
               }
               catch (JsonException ex)
               {
                  Log.WriteLog(LogLevel.WARNING, $"Offering file with content: {messageParts[1]} received and but not valid! " + ex.Message);
               }
            }
         }
         return succes;
      }

      public static bool EvaluateNodeListFileMessage(byte[] buffer, long offset, long size, out Dictionary<string, Node> NodeDict)
      {

         NodeDict = new Dictionary<string, Node>();
         bool succes = false;

         // Message has 2 parts: FLAG, NODE_LIST_FILE_ON_JSON_FORMAT
         string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
         string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

         if (messageParts.Length == 2)
         {
            try
            {
               NodeDict = JsonSerializer.Deserialize<Dictionary<string, Node>>(messageParts[1]);
               succes = true;
            }
            catch (JsonException ex)
            {
               Log.WriteLog(LogLevel.WARNING, $"Node list file with content: {messageParts[1]} received but not valid! " + ex.Message);
            }
         }
         return succes;
      }

      public static bool EvaluateNodeListRequestMessage(byte[] buffer, long offset, long size, [MaybeNullWhen(false)] out Node senderNode)
      {
         bool succes = false;
         senderNode = null;

         // Message has 2 parts: FLAG, NODE_ON_JSON_FORMAT
         string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
         string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

         if (messageParts.Length == 2)
         {
            try
            {
               senderNode = JsonSerializer.Deserialize<Node>(messageParts[1]);
               succes = true;
            }
            catch (JsonException ex)
            {
               Log.WriteLog(LogLevel.WARNING, $"Node request with content: {messageParts[1]} received but not valid! " + ex.Message);
            }
         }
         return succes;
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
