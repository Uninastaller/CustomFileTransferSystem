using Dapper;
using System;
using System.Data;

namespace SqliteClassLibrary
{
   public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
   {
      public override void SetValue(IDbDataParameter parameter, Guid guid)
      { }

      public override Guid Parse(object value)
      {
         return Guid.Parse((string)value);
      }
   }
}
