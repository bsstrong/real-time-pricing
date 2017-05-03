using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using com.LoanTek.API.OwinWrapper.ConsoleInterfaces;
using Topshelf;

namespace com.LoanTek.API.OwinWrapper
{
    class Program
    {
        // ReSharper disable once InconsistentNaming
        static void Main(string[] args)
        {
            //Console.WriteLine("Environment.UserInteractive:" + Environment.UserInteractive);
            //Console.WriteLine("args:" + args?.FirstOrDefault());
            //Thread.Sleep(2000);

            //TODO - FYI only setup to work with Partner Pricing...
            new Pricing.Partners.DataConnections();

            Global.Config = new Config().LoadConfig();
  
            if (!Environment.UserInteractive || (args?.Any() ?? false))
            {
                //Debug.Listeners.Clear();
                Service.RunService(args?.FirstOrDefault());
            }
            else
            {
                new RootInterface();    
            }
        }

      
        
       
        private static void loadServiceInterface()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Commands:");
            Console.WriteLine("\n 'start' = run the service within the console app.");
            Console.WriteLine("\n 'install' = install the service.");
            Console.WriteLine("\n 'uninstall' = uninstall the service.");
            Console.WriteLine("\n 'exit' = exit the service interface.");
            Console.WriteLine("Commands include: info, cache. i.e. Bankrate cache or Zillow info");
            Console.CursorVisible = true;

            while (true)
            {   
                Console.Write("Type a command > ");
                var s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                Console.ForegroundColor = ConsoleColor.DarkYellow;

                if (s.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Global.OnExit();
                    Thread.Sleep(3000);
                    break;
                }
                if (s.Equals("start", StringComparison.OrdinalIgnoreCase))
                {
                    runService();
                }
                else if (s.StartsWith("install", StringComparison.OrdinalIgnoreCase) || s.StartsWith("uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    runService(s);
                }
            }
        }

        private static void runService(string args = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Service starting...");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("loading data... please wait...");

            HostFactory.Run(x =>
            {
                x.Service<Service>(s =>
                {
                    s.ConstructUsing(xx => new Service());
                    s.WhenStarted(xx => xx.Start());
                    s.WhenStopped(xx => xx.Stop());
                });
                if (args != null)
                {
                    x.ApplyCommandLine(args);
                }
                else
                    x.RunAsLocalSystem();

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                    //rc.RestartComputer(1, "System is restarting!"); // restart the system after 1 minute
                    //rc.RunProgram(1, "notepad.exe"); // run a program
                    rc.SetResetPeriod(1); // set the reset interval to one day
                });

                x.SetDescription("LoanTek API Owin Wrapper. Use to self host the API web services.");
                x.SetDisplayName("LoanTek API Owin Wrapper");
                x.SetServiceName("LoanTek-API-Owin-Wrapper");
            });
            
        }

    }

}
