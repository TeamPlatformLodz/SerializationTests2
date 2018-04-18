using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            ConsoleColor color = ConsoleColor.White;
            if (logLevel == LogLevel.Critical)
            {
                color = ConsoleColor.Red;
            }
            else if (logLevel == LogLevel.Warning)
            {
                color = ConsoleColor.Yellow;
            }
            else if (logLevel == LogLevel.BusinessLogic)
            {
                color = ConsoleColor.DarkBlue;
            }
            Console.WriteLine(message, color);
            Console.Read();
        }
    }
}
