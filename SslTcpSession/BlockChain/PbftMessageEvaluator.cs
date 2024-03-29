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
           [MaybeNullWhen(false)] out Block receivedBlock, [MaybeNullWhen(false)] out string synchronizationHash)
        {
            bool succes = false;
            receivedBlock = null;
            synchronizationHash = null;

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

                    synchronizationHash = messageParts[2];

                    succes = true;
                }
                catch (JsonException ex)
                {
                    Log.WriteLog(LogLevel.WARNING, $"Block request with content: {messageParts[1]} received but not valid! " + ex.Message);
                }
            }
            return succes;
        }


        public static bool EvaluatePbftPrePrepareMessage(byte[] buffer, long offset, long size,
             [MaybeNullWhen(false)] out Block receivedBlock, [MaybeNullWhen(false)] out string signOfPrimaryReplica,
             [MaybeNullWhen(false)] out string synchronizationHash)
        {
            bool succes = false;
            receivedBlock = null;
            synchronizationHash = null;
            signOfPrimaryReplica = null;

            // Message has 4 parts: FLAG, BLOCK AS JSON,SIGN OF PRIMARY REPLICA, HASH OF ACTIVE REPLICAS
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

            if (messageParts.Length == 4)
            {
                try
                {
                    receivedBlock = JsonSerializer.Deserialize<Block>(messageParts[1]);
                    if (receivedBlock == null)
                    {
                        return false;
                    }

                    signOfPrimaryReplica = messageParts[2];
                    synchronizationHash = messageParts[3];

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