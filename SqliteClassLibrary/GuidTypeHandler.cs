using Dapper;
using Logger;
using System;
using System.Data;
using System.Windows.Controls;

namespace SqliteClassLibrary
{
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid guid)
        {  }

        public override Guid Parse(object value)
        {
            return Guid.Parse((string)value);
        }
    }
}
