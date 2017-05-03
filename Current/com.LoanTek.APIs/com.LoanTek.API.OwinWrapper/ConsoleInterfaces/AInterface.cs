using System;
using System.Collections.Generic;

namespace com.LoanTek.API.OwinWrapper.ConsoleInterfaces
{
    public abstract class AInterface
    {
        protected ConsoleColor ForegroundColor = ConsoleColor.White;
        protected ConsoleColor HighlightColor = ConsoleColor.DarkYellow;
        protected ConsoleColor SoftColor = ConsoleColor.Gray;

        public string Title { get; set; }
        public virtual void WriteTitle()
        {
            Console.ForegroundColor = HighlightColor;
            Console.WriteLine("\n"+ this.Title ?? string.Empty);
        }

        public List<string> MenuCommands { get; set; }
        public virtual void WriteMenu() 
        {
            if(this.MenuCommands?.Count == 0)
                return;
            Console.ForegroundColor = SoftColor;
            Console.WriteLine("Commands:");
            foreach (var command in this.MenuCommands)
            {
                Console.WriteLine(command); 
            }
        }

        public virtual void Interface()  
        {
            while (true)
            {
                this.WriteTitle();
                this.WriteMenu();

                Console.ForegroundColor = this.ForegroundColor;
                Console.Write("Type a command > ");
                Console.CursorVisible = true;
                var s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s))
                    continue;
                if (this.ProcessInput(s))
                    break;
            }
        }

        protected abstract bool ProcessInput(string s);

    }   
}
