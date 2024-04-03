using Common.Model;
using ConfigManager;
using Dapper;
using Logger;
using SqliteClassLibrary;
using SslTcpSession.BlockChain;
using System;
using System.Collections.Generic;
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

         SqlMapper.AddTypeHandler(typeof(Guid), new GuidTypeHandler());

         Blockchain.StartApplication();
         await SqliteDataAccessReplicaLog.InsertNewBlockAsync(Blockchain.Chain[0]);
         Blockchain.LoadedChainFromDb(await SqliteDataAccessReplicaLog.GetAllBlocksAsync());

         Log.WriteLog(LogLevel.DEBUG, "START OF PROGRAM");
      }

      private void Application_Exit(object sender, ExitEventArgs e)
      {
         MyConfigManager.EndApplication();
         Log.WriteLog(LogLevel.DEBUG, "END OF PROGRAM");
         Log.EndApplication();
         Blockchain.EndAplication();
      }
   }
}
