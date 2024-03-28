using Common.Model;
using Logger;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace SslTcpSession.BlockChain
{
   public static class PbftMessageEvaluator
   {
      public static bool EvaluatePbftRequestMessage(byte[] buffer, long offset, long size,
         [MaybeNullWhen(false)] out Block receivedBlock,[MaybeNullWhen(false)] out string? hashOfActiveReplicas)
      {
         bool succes = false;
         receivedBlock = null;
         hashOfActiveReplicas = null;

         // Message has 3 parts: FLAG, BLOCK_IN_JSON_FORMAT, HASH OF ACTIVE REPLICAS
         string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
         string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

         if (messageParts.Length == 3)
         {
            try
            {
               receivedBlock = JsonSerializer.Deserialize<Block>(messageParts[1]);
               if (receivedBlock == null)
               {
                  return false;
               }

               hashOfActiveReplicas = messageParts[2];

               succes = true;
            }
            catch (JsonException ex)
            {
               Log.WriteLog(LogLevel.WARNING, $"Block request with content: {messageParts[1]} received but not valid! " + ex.Message);
            }
         }
         return succes;
      }
   }
}
