using Common.Enum;
using Common.Model;
using ConfigManager;

namespace Common.ThreadMessages
{
   public class NodeSettingWindowMessage : MsgBase<DisposeMessage>
   {
      public NodeSettingWindowMessage(Node node, NodeSettingsWindowState state) : base(typeof(NodeSettingWindowMessage))
      {
         Node = node;
         State = state;
      }

      public Node Node { get; set; }
      public NodeSettingsWindowState State { get; set; }

   }
}
