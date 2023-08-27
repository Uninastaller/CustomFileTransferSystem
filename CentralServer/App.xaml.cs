using Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CentralServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Log.StartApplication();
            //Log.WriteLog(LogLevel.DEBUG, "START OF PROGRAM");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            //Log.WriteLog(LogLevel.DEBUG, "END OF PROGRAM");
            //Log.EndApplication();
        }
    }
}
