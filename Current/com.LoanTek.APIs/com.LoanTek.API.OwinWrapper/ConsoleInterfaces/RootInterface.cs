using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Web.Configuration;

namespace com.LoanTek.API.OwinWrapper.ConsoleInterfaces
{
    public sealed class RootInterface : AInterface
    {    
        public RootInterface()
        {       
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WindowTop = 0;
            Console.WindowLeft = 0;
            Console.WindowWidth = Console.LargestWindowWidth - 25;
            Console.WindowHeight = Console.LargestWindowHeight - 25;

            //image
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(Global.LoanTekArt4);

            Console.WriteLine("\nVersion:"+ Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("\nDefault setting values:");
            Console.WriteLine("API AssemblyToUse: " + Global.Config.AssemblyName);
            Console.WriteLine("Host: " + Global.Config.Host);
            Console.WriteLine("Port: " + Global.Config.Port);
            Console.WriteLine("Status: " + Global.Status);

            this.Title = "Welcome to the LoanTek API Owin Wrapper";
            this.MenuCommands = new List<string>
            {
                "'config' = enter the configuration interface.",
                "'console' = enter the console interface.",
                "'service' = enter the service interface.",
                "'exit' = exit the application."
            };
            this.Interface();
        }

        protected override bool ProcessInput(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            if (s.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return this.exit();
            if (s.Equals("config", StringComparison.OrdinalIgnoreCase))
                new ConfigInterface();
            else if (s.Equals("console", StringComparison.OrdinalIgnoreCase))
                new ConsoleInterface();
            else if (s.Equals("service", StringComparison.OrdinalIgnoreCase))
                new ServiceInterface();
            else
                Console.WriteLine("sorry, that command is not recognized.");
            return false;
        }

        private bool exit()
        {
            Console.WriteLine("Goodbye!");
            Thread.Sleep(2000);
            return true;
        }
    }   
}
