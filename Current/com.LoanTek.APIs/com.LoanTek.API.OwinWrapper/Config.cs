using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using com.LoanTek.API.Pricing.Partners;
using LoanTek.LoggingObjects;
using Newtonsoft.Json;

namespace com.LoanTek.API.OwinWrapper
{
    public class Config
    {
        public string Host { get; set; } = Debugger.IsAttached ? "localhost" : "*";
        public int Port { get; set; } = 12345;
        public string AssemblyName { get; set; } = "com.LoanTek.API.Pricing.Partners.dll";
        public string CacheHost { get; set; } = "cachelb.loantek.com";
        public int CachePort { get; set; } = 6379;

        private string sqlHost;
        public string SqlHost
        {
            get { return this.sqlHost; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.sqlHost = value;

                    List<string> sections = DataConnections.DataContextQuoteSystemsWrite.Split(';').ToList();
                    sections[0] = "Data Source=" + this.sqlHost;
                    var conStr = string.Join(";", sections);
                    DataConnections.DataContextQuoteSystemsWrite = conStr;

                    sections = DataConnections.DataContextLoanTekRead.Split(';').ToList();
                    sections[0] = "Data Source=" + this.sqlHost;
                    conStr = string.Join(";", sections);
                    DataConnections.DataContextLoanTekRead = conStr;
                    SimpleLogger.DataConnectionString = conStr;
                }
            }
        }

        public Config LoadConfig(string pathToConfigFile = null)
        {
            string binPath = pathToConfigFile ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Config.json";
            if (File.Exists(binPath))
            {
                string json = File.ReadAllText(binPath);
                Config cfg = JsonConvert.DeserializeObject<Config>(json);
                this.Host = cfg.Host;
                this.Port = cfg.Port;
                this.AssemblyName = cfg.AssemblyName;
                this.CacheHost = cfg.CacheHost;
                this.CachePort = cfg.CachePort;
                this.SqlHost = cfg.SqlHost;
            }
            return this;
        }

        public Config SaveConfig(string pathToConfigFile = null)
        {
            string binPath = pathToConfigFile ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Config.json";
            if (File.Exists(binPath))
            {
                string json = JsonConvert.SerializeObject(this);
                File.WriteAllText(binPath, json);
            }
            if (Debugger.IsAttached)
            {
                binPath = pathToConfigFile ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\Config.json";
                if (File.Exists(binPath))
                {
                    string json = JsonConvert.SerializeObject(this);
                    File.WriteAllText(binPath, json);
                }
            }
            return this;
        }
    }

}
