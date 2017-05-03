using System;
using Microsoft.Owin.Hosting;
using Topshelf;

namespace com.LoanTek.API.OwinWrapper
{
    public class Service
    {
        public static string DisplayName = "LoanTek API Owin Wrapper";
        public static string ServiceName = "LoanTek-API-Owin-Wrapper";
        private IDisposable webapp; 

        public void Start()
        {
            webapp = WebApp.Start<Startup>(Global.FullHostName);
            Global.OnStart("Service startup...");
        }   
            
        public void Stop()
        {
            webapp?.Dispose();
            Global.OnExit("Service exiting...");
        }

        public static void RunService(string args = null)
        {
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
                x.SetDisplayName(DisplayName);
                x.SetServiceName(ServiceName);
            });

        }

        
    }
}
