using Common.Model;
using ConfigManager;
using Logger;
using System.Windows;

namespace Client
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   public partial class App : Application
   {
      private async void Application_Startup(object sender, StartupEventArgs e)
      {
         MyConfigManager.StartApplication();
         Log.StartApplication();
         NodeDiscovery.SetIpAddresses(NetworkUtils.GetLocalIPAddress(), await NetworkUtils.GetPublicIPAddress());
         NodeDiscovery.StartApplication();
         Log.WriteLog(LogLevel.DEBUG, "START OF PROGRAM");
      }

      private void Application_Exit(object sender, ExitEventArgs e)
      {
         MyConfigManager.EndApplication();
         Log.WriteLog(LogLevel.DEBUG, "END OF PROGRAM");
         Log.EndApplication();
      }
   }
}
