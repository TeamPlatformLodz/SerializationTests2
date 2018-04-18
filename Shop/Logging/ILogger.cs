using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Logging
{
    public interface ILogger
    {
        void Log(string message, LogLevel logLevel = LogLevel.Normal);
    }
}
