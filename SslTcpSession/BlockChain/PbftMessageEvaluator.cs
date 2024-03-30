﻿using Common.Model;
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
             [MaybeNullWhen(false)] out Block receivedBlock, out Guid primaryReplicaId, 
             [MaybeNullWhen(false)] out string signOfPrimaryReplica, [MaybeNullWhen(false)] out string synchronizationHash)
        {
            bool success = false;
            receivedBlock = null;
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
                    receivedBlock = JsonSerializer.Deserialize<Block>(messageParts[1]);
                    if (receivedBlock == null)
                    {
                        return false;
                    }

                    success = Guid.TryParse(messageParts[2], out primaryReplicaId);
                    signOfPrimaryReplica = messageParts[3];
                    synchronizationHash = messageParts[4];
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
                }
                catch (JsonException ex)
                {
                    Log.WriteLog(LogLevel.WARNING, $"Prepare with hash of request: {messageParts[1]} received but not valid! " + ex.Message);
                }
            }
            return success;
        }
    }
}