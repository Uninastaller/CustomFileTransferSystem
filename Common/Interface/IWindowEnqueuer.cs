using Common.Model;

namespace Common.Interface
{
   public interface IWindowEnqueuer
   {
      void BaseMsgEnque(BaseMsg baseMsg);
      bool IsOpen();
   }
}
