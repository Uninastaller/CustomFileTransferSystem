using Common.Model;
using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SslTcpSession.BlockChain
{
    public static class PbftMessageEvaluator
    {
        public static bool EvaluatePbftRequestMessage(byte[] buffer, long offset, long size, [MaybeNullWhen(false)] out Block receivedBlock)
        {
            bool succes = false;
            receivedBlock = null;

            // Message has 2 parts: FLAG, BLOCK_IN_JSON_FORMAT
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(FlagMessagesGenerator.messageConnector, StringSplitOptions.None);

            if (messageParts.Length == 2)
            {
                try
                {
                    receivedBlock = JsonSerializer.Deserialize<Block>(messageParts[1]);
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
