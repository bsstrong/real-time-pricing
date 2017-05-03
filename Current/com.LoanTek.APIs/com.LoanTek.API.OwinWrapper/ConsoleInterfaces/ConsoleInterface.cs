using System;
using System.Threading;
using Microsoft.Owin.Hosting;
using System.Collections.Generic;

namespace com.LoanTek.API.OwinWrapper.ConsoleInterfaces
{
    public sealed class ConsoleInterface : AInterface
    {
        private IDisposable webapp;

        public override void WriteTitle()
        {
            Console.ForegroundColor = HighlightColor;
            Console.WriteLine("\n" + (this.Title ?? string.Empty) +" Status:"+ Global.Status);
        }

        public ConsoleInterface()
        {
            this.Title = "You are in the Console Interface.";
            this.MenuCommands = new List<string>();
            this.MenuCommands.Add("'start' = start the Owin Self Host.");
            this.MenuCommands.Add("'stop' = stop the Owin Self Host.");
            this.MenuCommands.Add("'exit' = exit the console interface.");       
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
            else
                Console.WriteLine("sorry, that command is not recognized.");
            return false;
        }

        private void start()
        {
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("loading data... please wait...");

            //using (this.webapp = WebApp.Start<Startup>(Global.FullHostName))
            {
                webapp = WebApp.Start<Startup>(Global.FullHostName);
                var msg = Global.OnStart();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(msg);
                //Console.ForegroundColor = ConsoleColor.Gray;
                //Console.WriteLine("Commands should be prefixed with the QuotingChannel / Controller name. i.e. Bankrate or Zillow");
                //Console.WriteLine("Commands include: info, cache. i.e. Bankrate cache or Zillow info");
                //Console.ForegroundColor = this.ForegroundColor;
                Console.CursorVisible = true;
                Global.Status = "running";
            }
        }

        private void stop()
        {
            this.webapp?.Dispose();
            this.webapp = null;
            var msg = Global.OnExit();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Thread.Sleep(1000);
            Global.Status = "stopped";
        }

        private bool exit()
        {
            if (this.webapp != null)
                this.stop();
            return true;
        }
    }
}
