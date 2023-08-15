using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using Modeel.Log;

namespace Modeel.Frq
{
    public class NumberSwitch
   {
      private Dictionary<int, Action<object>> numActionMapper = new Dictionary<int, Action<object>>();

      public NumberSwitch Case<T>(int num, Action<T> action)
      {
         try
         {
            numActionMapper.Add(num, delegate (object obj)
            {
               action((T)obj);
            });
            return this;
         }
         catch (ArgumentException ex)
         {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("{0}, {1}, {2}, {3}", num, action.Target, action.Method, ex.Message);
            Logger.WriteLog(LogLevel.ERROR, $"{ex.Message}; {stringBuilder}");
            return this;
         }
         catch (Exception ex2)
         {
            Logger.WriteLog(LogLevel.ERROR, ex2.Message);
            return this;
         }
      }

      public void Switch(int num, object obj)
      {
         try
         {
            numActionMapper[num](obj);
         }
         catch (KeyNotFoundException ex)
         {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("{0} {1}", num, ex.Message);
            Logger.WriteLog(LogLevel.ERROR, $"{ex.Message}; {stringBuilder}");
         }
         catch (Exception ex2)
         {
            StringBuilder stringBuilder2 = new StringBuilder();
            stringBuilder2.AppendFormat("{0} {1}", num, ex2.Message);
            Logger.WriteLog(LogLevel.DEBUG, $"{ex2.Message}; {stringBuilder2}");
            throw new NotImplementedException(stringBuilder2.ToString());
         }
      }
   }
}
