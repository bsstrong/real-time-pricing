using System.Diagnostics;
using LoanTek.LoggingObjects;

namespace com.LoanTek.API
{
    public class Logger : SimpleLogger
    {
        public Logger(LogToType logTo) : base (logTo)
        {
            if (Debugger.IsAttached)
            {
                this.LogToDebuggerOnAll = true;
                //this.LogTo = LogToType.DEBUG; //if Debugger IsAttached  ignore LogTo value and only log to the debugger
            }
        }
    }
}