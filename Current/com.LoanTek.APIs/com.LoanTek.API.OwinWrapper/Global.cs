using System;
using System.Diagnostics;
using LoanTek.LoggingObjects;

namespace com.LoanTek.API.OwinWrapper
{
    public struct Global
    {
        public static Config Config { get; set; }

        public static string Status = "stopped";

        public static string FullHostName => (Config.Host.StartsWith("http://") ? Config.Host : "http://"+ Config.Host) + ":" + Config.Port;
        

        public static string OnStart(string msg = null)
        {
            string s = (msg ?? "Welcome to the LoanTek API Owin Wrapper.") + " Listening at " + FullHostName + " for requests...";
            OutPrint(SimpleLogger.LogLevelType.INFO, s, "OnStart");
            return s;
        }

        public static string OnExit(string msg = null)
        {
            string s = (msg ?? string.Empty) + "...exiting " + FullHostName + ". Goodbye!";
            OutPrint(SimpleLogger.LogLevelType.WARNING, s, "OnExit");
            return s;
        }

        private static readonly SimpleLogger errorLogger = new SimpleLogger(Debugger.IsAttached ? SimpleLogger.LogToType.DEBUG : Environment.UserInteractive ? SimpleLogger.LogToType.CONSOLE : SimpleLogger.LogToType.DATABASE);
        public static void OutPrint(SimpleLogger.LogLevelType logLevel, string msg, string fromMethod, string fromClass = null, string fromNamespace = null)
        {
            var locationObject = new SimpleLogger.LocationObject
            {
                Namespace = fromNamespace ?? "com.LoanTek.API.OwinWrapper",
                ClassName = fromClass ?? "Global",
                MethodName = fromMethod
            };
            errorLogger.Log(logLevel, msg, locationObject);
        }


        #region LoanTek name art

        public static string LoanTekArt1 = "   _                    _______   _    \r\n | |                  |__   __| | |   \r\n | |     ___   __ _ _ __ | | ___| | __\r\n | |    / _ \\ / _` | \'_ \\| |/ _ \\ |/ /\r\n | |___| (_) | (_| | | | | |  __/   < \r\n |______\\___/ \\__,_|_| |_|_|\\___|_|\\_\\\r\n                                      \r\n                                      ";
        public static string LoanTekArt2 = "  __        ______        ___      .__   __. .___________. _______  __  ___ \r\n|  |      /  __  \\      /   \\     |  \\ |  | |           ||   ____||  |/  / \r\n|  |     |  |  |  |    /  ^  \\    |   \\|  | `---|  |----`|  |__   |  \'  /  \r\n|  |     |  |  |  |   /  /_\\  \\   |  . `  |     |  |     |   __|  |    <   \r\n|  `----.|  `--\'  |  /  _____  \\  |  |\\   |     |  |     |  |____ |  .  \\  \r\n|_______| \\______/  /__/     \\__\\ |__| \\__|     |__|     |_______||__|\\__\\ \r\n                                                                           ";
        public static string LoanTekArt3 = " __         ______     ______     __   __     ______   ______     __  __    \r\n/\\ \\       /\\  __ \\   /\\  __ \\   /\\ \"-.\\ \\   /\\__  _\\ /\\  ___\\   /\\ \\/ /    \r\n\\ \\ \\____  \\ \\ \\/\\ \\  \\ \\  __ \\  \\ \\ \\-.  \\  \\/_/\\ \\/ \\ \\  __\\   \\ \\  _\"-.  \r\n \\ \\_____\\  \\ \\_____\\  \\ \\_\\ \\_\\  \\ \\_\\\\\"\\_\\    \\ \\_\\  \\ \\_____\\  \\ \\_\\ \\_\\ \r\n  \\/_____/   \\/_____/   \\/_/\\/_/   \\/_/ \\/_/     \\/_/   \\/_____/   \\/_/\\/_/ \r\n                                                                            ";
        public static string LoanTekArt4 = "           _____           _______                   _____                    _____                _____                    _____                    _____          \r\n         /\\    \\         /::\\    \\                 /\\    \\                  /\\    \\              /\\    \\                  /\\    \\                  /\\    \\         \r\n        /::\\____\\       /::::\\    \\               /::\\    \\                /::\\____\\            /::\\    \\                /::\\    \\                /::\\____\\        \r\n       /:::/    /      /::::::\\    \\             /::::\\    \\              /::::|   |            \\:::\\    \\              /::::\\    \\              /:::/    /        \r\n      /:::/    /      /::::::::\\    \\           /::::::\\    \\            /:::::|   |             \\:::\\    \\            /::::::\\    \\            /:::/    /         \r\n     /:::/    /      /:::/~~\\:::\\    \\         /:::/\\:::\\    \\          /::::::|   |              \\:::\\    \\          /:::/\\:::\\    \\          /:::/    /          \r\n    /:::/    /      /:::/    \\:::\\    \\       /:::/__\\:::\\    \\        /:::/|::|   |               \\:::\\    \\        /:::/__\\:::\\    \\        /:::/____/           \r\n   /:::/    /      /:::/    / \\:::\\    \\     /::::\\   \\:::\\    \\      /:::/ |::|   |               /::::\\    \\      /::::\\   \\:::\\    \\      /::::\\    \\           \r\n  /:::/    /      /:::/____/   \\:::\\____\\   /::::::\\   \\:::\\    \\    /:::/  |::|   | _____        /::::::\\    \\    /::::::\\   \\:::\\    \\    /::::::\\____\\________  \r\n /:::/    /      |:::|    |     |:::|    | /:::/\\:::\\   \\:::\\    \\  /:::/   |::|   |/\\    \\      /:::/\\:::\\    \\  /:::/\\:::\\   \\:::\\    \\  /:::/\\:::::::::::\\    \\ \r\n/:::/____/       |:::|____|     |:::|    |/:::/  \\:::\\   \\:::\\____\\/:: /    |::|   /::\\____\\    /:::/  \\:::\\____\\/:::/__\\:::\\   \\:::\\____\\/:::/  |:::::::::::\\____\\\r\n\\:::\\    \\        \\:::\\    \\   /:::/    / \\::/    \\:::\\  /:::/    /\\::/    /|::|  /:::/    /   /:::/    \\::/    /\\:::\\   \\:::\\   \\::/    /\\::/   |::|~~~|~~~~~     \r\n \\:::\\    \\        \\:::\\    \\ /:::/    /   \\/____/ \\:::\\/:::/    /  \\/____/ |::| /:::/    /   /:::/    / \\/____/  \\:::\\   \\:::\\   \\/____/  \\/____|::|   |          \r\n  \\:::\\    \\        \\:::\\    /:::/    /             \\::::::/    /           |::|/:::/    /   /:::/    /            \\:::\\   \\:::\\    \\            |::|   |          \r\n   \\:::\\    \\        \\:::\\__/:::/    /               \\::::/    /            |::::::/    /   /:::/    /              \\:::\\   \\:::\\____\\           |::|   |          \r\n    \\:::\\    \\        \\::::::::/    /                /:::/    /             |:::::/    /    \\::/    /                \\:::\\   \\::/    /           |::|   |          \r\n     \\:::\\    \\        \\::::::/    /                /:::/    /              |::::/    /      \\/____/                  \\:::\\   \\/____/            |::|   |          \r\n      \\:::\\    \\        \\::::/    /                /:::/    /               /:::/    /                                 \\:::\\    \\                |::|   |          \r\n       \\:::\\____\\        \\::/____/                /:::/    /               /:::/    /                                   \\:::\\____\\               \\::|   |          \r\n        \\::/    /         ~~                      \\::/    /                \\::/    /                                     \\::/    /                \\:|   |          \r\n         \\/____/                                   \\/____/                  \\/____/                                       \\/____/                  \\|___|          \r\n                                                                                                                                                                   ";

        #endregion
    }
}
