using System;

namespace Common.Model
{
    public class MsgBase<T> : BaseMsg
    {
        public MsgBase(Type type)
           : base(type)
        {
        }
    }
}