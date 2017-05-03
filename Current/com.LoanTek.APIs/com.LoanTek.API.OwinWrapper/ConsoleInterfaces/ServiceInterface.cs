using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace com.LoanTek.API.OwinWrapper.ConsoleInterfaces
{
    public sealed class ServiceInterface : AInterface
    {
        private IDisposable webapp;
        private ServiceController service;

        public override void WriteTitle()
        {
            ServiceController[] services = ServiceController.GetServices();
            this.service = services.FirstOrDefault(s => s.ServiceName == Service.ServiceName);
            Global.Status = service?.Status.ToString() ?? "stopped";

            Console.ForegroundColor = HighlightColor;
            Console.WriteLine("\n" + (this.Title ?? string.Empty) +" Status:"+ Global.Status);
            Console.WriteLine("Service " + Service.ServiceName + " is " + (this.service == null ? "NOT " : string.Empty) + "installed. ");
        }

        public ServiceInterface()
        {
            this.Title = "You are in the Service Interface.";
            this.MenuCommands = new List<string>();
            this.MenuCommands.Add("'install' = install the Owin Self Host service.");
            this.MenuCommands.Add("'uninstall' = uninstall the Owin Self Host service.");
            this.MenuCommands.Add("'start' = start the Owin Self Host service in console.");
            this.MenuCommands.Add("'crtl + c' = stop the Owin Self Host console service.");
            this.MenuCommands.Add("'exit' = exit the service interface.");       
            this.Interface();
        }

        protected override bool ProcessInput(string s)
        { 
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            if (s.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return this.exit();
            if (s.Equals("start", StringComparison.OrdinalIgnoreCase))
                this.start();
            else if (s.Equals("stop", StringComparison.OrdinalIgnoreCase))
                this.stop();
            else if (s.StartsWith("install", StringComparison.OrdinalIgnoreCase))
                this.start(s +" start"); //auto start after install...
            else if (s.StartsWith("uninstall", StringComparison.OrdinalIgnoreCase))
                this.start(s);
            else
                Console.WriteLine("sorry, that command is not recognized.");
            return false;
        }

        private void start(string args = null)
        {
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("loading data... please wait...");
            Service.RunService(args);
            Global.Status = (this.webapp != null || this.service?.Status == ServiceControllerStatus.Running) ? "running" : "stopped";
        }

        private void stop()
        {
            this.webapp?.Dispose();
            this.webapp = null;
            var msg = Global.OnExit();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Thread.Sleep(1000);
            Global.Status = this.service?.Status == ServiceControllerStatus.Running ? "running" : "stopped";
        }
    
        private bool exit()
        {
            if (this.webapp != null)
                this.stop();
            return true;
        }
    }
}
