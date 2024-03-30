using Common.Enum;
using Common.Model;
using ConfigManager;
using Logger;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace SslTcpSession.BlockChain
{
    public static class PbftMessageEvaluator
    {

        public delegate void ReceivePbftMessageEventHandler(PbftReplicaLogDto log);
        public static event ReceivePbftMessageEventHandler? ReceivePbftMessage;

        private static void OnReceivePbftMessage(PbftReplicaLogDto log)
        {
            ReceivePbftMessage?.Invoke(log);
        }

        public static bool EvaluatePbftRequestMessage(byte[] buffer, long offset, long size,
           [MaybeNullWhen(false)] out Block requestedBlock, [MaybeNullWhen(false)] out string synchronizationHash)
        {
            bool succes = false;
            requestedBlock = null;
            synchronizationHash = null;

            // Message has 3 parts: FLAG, BLOCK_IN_JSON_FORMAT, HASH OF ACTIVE REPLICAS
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

            if (messageParts.Length == 3)
            {
                try
                {
                    requestedBlock = JsonSerializer.Deserialize<Block>(messageParts[1]);
                    if (requestedBlock == null)
                    {
                        return false;
                    }

                    synchronizationHash = messageParts[2];

                    // Send signal to gui to create log
                    OnReceivePbftMessage(new PbftReplicaLogDto(SocketMessageFlag.PBFT_REQUEST, MessageDirection.RECEIVED,
                        synchronizationHash, requestedBlock.Hash, NodeDiscovery.GetMyNode().Id.ToString(), requestedBlock.NodeId.ToString(), DateTime.UtcNow));

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
             [MaybeNullWhen(false)] out Block requestedBlock, out Guid primaryReplicaId, 
             [MaybeNullWhen(false)] out string signOfPrimaryReplica, [MaybeNullWhen(false)] out string synchronizationHash)
        {
            bool success = false;
            requestedBlock = null;
            synchronizationHash = null;
            signOfPrimaryReplica = null;
            primaryReplicaId = Guid.Empty;

            // Message has 5 parts: FLAG, BLOCK AS JSON, PRIMARY REPLICA ID, SIGN OF PRIMARY REPLICA, HASH OF ACTIVE REPLICAS
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

            if (messageParts.Length == 5)
            {
                try
                {
                    requestedBlock = JsonSerializer.Deserialize<Block>(messageParts[1]);
                    if (requestedBlock == null)
                    {
                        return false;
                    }

                    success = Guid.TryParse(messageParts[2], out primaryReplicaId);
                    signOfPrimaryReplica = messageParts[3];
                    synchronizationHash = messageParts[4];

                    if (success)
                    {
                        // Send signal to gui to create log
                        OnReceivePbftMessage(new PbftReplicaLogDto(SocketMessageFlag.PBFT_PRE_PREPARE, MessageDirection.RECEIVED,
                            synchronizationHash, requestedBlock.Hash, NodeDiscovery.GetMyNode().Id.ToString(), primaryReplicaId.ToString(), DateTime.UtcNow));
                    }
                }
                catch (JsonException ex)
                {
                    Log.WriteLog(LogLevel.WARNING, $"Pre-prepare with content: {messageParts[1]} received but not valid! " + ex.Message);
                }
            }
            return success;
        }

        public static bool EvaluatePbftPrepareMessage(byte[] buffer, long offset, long size,
             [MaybeNullWhen(false)] out string hashOfRequest, [MaybeNullWhen(false)] out string signOfBackupReplica,
             [MaybeNullWhen(false)] out string synchronizationHash, out Guid guidOfBackupReplica)
        {
            bool success = false;
            hashOfRequest = null;
            signOfBackupReplica = null;
            synchronizationHash = null;
            guidOfBackupReplica = Guid.Empty;

            // Message has 5 parts: FLAG, HASH OF REQUEST,SIGN OF BACKUP REPLICA, HASH OF ACTIVE REPLICAS, GUID OF BACKUP REPLICA
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

            if (messageParts.Length == 5)
            {
                try
                {
                    hashOfRequest = messageParts[1];
                    signOfBackupReplica = messageParts[2];
                    synchronizationHash = messageParts[3];
                    success = Guid.TryParse(messageParts[4], out guidOfBackupReplica);

                    if (success)
                    {
                        // Send signal to gui to create log
                        OnReceivePbftMessage(new PbftReplicaLogDto(SocketMessageFlag.PBFT_PREPARE, MessageDirection.RECEIVED,
                            synchronizationHash, hashOfRequest, NodeDiscovery.GetMyNode().Id.ToString(), guidOfBackupReplica.ToString(), DateTime.UtcNow));
                    }
                }
                catch (JsonException ex)
                {
                    Log.WriteLog(LogLevel.WARNING, $"Prepare with hash of request: {messageParts[1]} received but not valid! " + ex.Message);
                }
            }
            return success;
        }

        public static bool EvaluatePbftErrorMessage(byte[] buffer, long offset, long size,
             [MaybeNullWhen(false)] out string hashOfRequest, [MaybeNullWhen(false)] out string synchronizationHash,
             [MaybeNullWhen(false)] out string errorMessage, out Guid guidOfSender)
        {
            bool success = false;
            hashOfRequest = null;
            synchronizationHash = null;
            errorMessage = null;
            guidOfSender = Guid.Empty;

            // Message has 5 parts: FLAG, HASH OF REQUEST, HASH OF ACTIVE REPLICAS, ERROR MESSAGE, GUID OF SENDER
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

            if (messageParts.Length == 5)
            {
                try
                {
                    hashOfRequest = messageParts[1];
                    synchronizationHash = messageParts[2];
                    errorMessage = messageParts[3];
                    success = Guid.TryParse(messageParts[4], out guidOfSender);

                    if (success)
                    {
                        // Send signal to gui to create log
                        OnReceivePbftMessage(new PbftReplicaLogDto(SocketMessageFlag.PBFT_PREPARE, MessageDirection.RECEIVED,
                            synchronizationHash, hashOfRequest, NodeDiscovery.GetMyNode().Id.ToString(), guidOfSender.ToString(), DateTime.UtcNow, errorMessage));
                    }                    
                }
                catch (JsonException ex)
                {
                    Log.WriteLog(LogLevel.WARNING, $"Error with hash of request: {messageParts[1]} received but not valid! " + ex.Message);
                }
            }
            return success;
        }
    }
}