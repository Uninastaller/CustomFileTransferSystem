using Logger;
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
            Log.StartApplication();
            Log.WriteLog(LogLevel.DEBUG, "START OF PROGRAM");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.WriteLog(LogLevel.DEBUG, "END OF PROGRAM");
            Log.EndApplication();
        }
    }
}
