using Modeel.Log;
using System.Windows;

namespace Modeel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Logger.StartApplication();
            Logger.WriteLog(LogLevel.Debug, "START OF PROGRAM");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Logger.WriteLog(LogLevel.Debug, "END OF PROGRAM");
            Logger.EndApplication();
        }
    }
}
