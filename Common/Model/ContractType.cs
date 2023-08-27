using Common.Interface;
using Logger;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Model
{
    public class ContractType : IContractType
    {
        private SortedDictionary<int, Type> contractIdsMap = new SortedDictionary<int, Type>();

        private SortedDictionary<string, int> contractTypesMap = new SortedDictionary<string, int>();

        private static ContractType? contractType = null;

        private static readonly object syncRoot = new object();

        public Type GetContractType(int contractId)
        {
            lock (syncRoot)
            {
                return contractIdsMap[contractId];
            }
        }

        public int GetContractId(Type contractType)
        {
            int value = -1;

            if (contractType.FullName != null)
            {
                lock (syncRoot)
                {
                    contractTypesMap.TryGetValue(contractType.FullName, out value);
                    return value;
                }
            }
            return value;
        }

        public ContractType()
        {
            //Add(MsgIds.TestMessage, typeof(NewClientConnectedMessage));
        }

        public void Add(int msgId, Type t)
        {
            lock (syncRoot)
            {
                if (!contractIdsMap.ContainsKey(msgId) && t.FullName != null)
                {
                    contractIdsMap.Add(msgId, t);
                    contractTypesMap.Add(t.FullName, msgId);
                }
                else
                {
                    Log.WriteLog(LogLevel.WARNING, $"Already exists ContractId:{msgId} Type:{t.FullName}");
                }
            }
        }

        public static IContractType GetInstance()
        {
            if (contractType == null)
            {
                lock (syncRoot)
                {
                    if (contractType == null)
                    {
                        contractType = new ContractType();
                    }
                }
            }

            return contractType;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            lock (syncRoot)
            {
                foreach (KeyValuePair<int, Type> item in contractIdsMap)
                {
                    stringBuilder.AppendFormat($"{item.Key}:{item.Value.FullName}\n");
                }
            }

            return stringBuilder.ToString();
        }
    }
}
