namespace Modeel.Frq
{
   public interface IWindowEnqueuer
   {
      void BaseMsgEnque(BaseMsg baseMsg);
      bool IsOpen();
   }
}
