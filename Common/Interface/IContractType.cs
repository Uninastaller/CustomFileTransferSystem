using System;

namespace Common.Interface
{
    public interface IContractType
    {
        Type GetContractType(int contractTypeId);

        int GetContractId(Type contractType);

        void Add(int msgId, Type t);
    }
}
