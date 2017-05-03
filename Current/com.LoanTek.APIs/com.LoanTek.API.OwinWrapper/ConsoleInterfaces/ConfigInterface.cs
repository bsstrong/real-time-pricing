using System;
using System.Collections.Generic;
using System.Linq;

namespace com.LoanTek.API.OwinWrapper.ConsoleInterfaces
{
    public sealed class ConfigInterface : AInterface
    {
        public override void WriteTitle()
        {
            Console.ForegroundColor = HighlightColor;
            Console.WriteLine("\n" + (this.Title ?? string.Empty));
            //Console.ForegroundColor = ForegroundColor;
            Console.WriteLine("SqlHost: " + Global.Config.SqlHost);
            Console.WriteLine("CacheHost: " + Global.Config.CacheHost);
            Console.WriteLine("CachePort: " + Global.Config.CachePort);
            Console.WriteLine("Host: " + Global.Config.Host);
            Console.WriteLine("Port: " + Global.Config.Port);
            Console.WriteLine("FullHostName: " + Global.FullHostName);         
        }

        public ConfigInterface()
        {
            this.Title = "You are in the Configuration Interface.";

            this.MenuCommands = new List<string>();
            this.MenuCommands.Add("'host theHostName' = type 'host' followed by a host name to set the host to this value.");
            this.MenuCommands.Add("'port thePortNumber' = type 'port' followed by a port number to set the port to this value.");
            this.MenuCommands.Add("'cachehost theCacheHostName' = type 'cachehost' followed by a host name to set the distributed cache host to this value.");
            this.MenuCommands.Add("'cacheport theCachePortNumber' = type 'cacheport' followed by a port number to set the distributed cache port to this value.");
            this.MenuCommands.Add("'sqlhost theSqlHostName' = type 'sqlhost' followed by a sql host name to update the sql connection string with this value.");
            this.MenuCommands.Add("'exit' = exit the configuration interface.");       
            this.Interface();
        }

        protected override bool ProcessInput(string s)
        { 
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            s = s.Trim();
            if (s.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return this.exit();
            if (s.StartsWith("host ", StringComparison.OrdinalIgnoreCase))
                this.setHost(s);
            else if (s.StartsWith("port ", StringComparison.OrdinalIgnoreCase))
                this.setPort(s);
            else if (s.StartsWith("cachehost ", StringComparison.OrdinalIgnoreCase))
                this.setCacheHost(s);
            else if (s.StartsWith("cacheport ", StringComparison.OrdinalIgnoreCase))
                this.setCachePort(s);
            else if (s.StartsWith("sqlhost ", StringComparison.OrdinalIgnoreCase))
                this.setSqlHost(s);
            else
                Console.WriteLine("sorry, that command is not recognized.");
            return false;
        }

        private void setHost(string s)
        {
            var ss = s.Split(' ').LastOrDefault();
            if(string.IsNullOrEmpty(ss))
                Console.WriteLine("Invalid hostvalue:" + ss);
            Global.Config.Host = ss;
            Global.Config.SaveConfig();
            setResponse("Host set to:"+ ss);
        }

        private void setPort(string s)
        {
            int ss = NullSafe.NullSafeInteger(s.Split(' ').LastOrDefault());
            if (ss < 1)
                Console.WriteLine("Invalid portvalue:" + ss);
            Global.Config.Port = ss;
            Global.Config.SaveConfig();
            setResponse("Port set to:" + ss);
        }

        private void setCacheHost(string s)
        {
            var ss = s.Split(' ').LastOrDefault();
            if (string.IsNullOrEmpty(ss))
                Console.WriteLine("Invalid cachehost value:" + ss);
            Global.Config.CacheHost = ss;
            Global.Config.SaveConfig();
            setResponse("Cache Host set to:" + ss);
        }

        private void setCachePort(string s)
        {
            int ss = NullSafe.NullSafeInteger(s.Split(' ').LastOrDefault());
            if (ss < 1)
                Console.WriteLine("Invalid cacheport value:" + ss);
            Global.Config.CachePort = ss;
            Global.Config.SaveConfig();
            setResponse("Cache Port set to:" + ss);   
        }

        private void setSqlHost(string s)
        {
            var ss = s.Split(' ').LastOrDefault();
            if (string.IsNullOrEmpty(ss))
                Console.WriteLine("Invalid sqlhost value:" + ss);
            Global.Config.SqlHost = ss;
            Global.Config.SaveConfig();
            setResponse("Sql Host set to:" + ss);
        }
            
        private void setResponse(string s)
        {
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(s);
        }

        private bool exit()
        {
            return true;
        }
    }
}
