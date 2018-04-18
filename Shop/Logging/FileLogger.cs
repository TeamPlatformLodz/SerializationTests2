using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Logging
{
    public class FileLogger : ILogger
    {
        private string pathFile = "";

        public FileLogger(string pathToLogFile)
        {
            if(! File.Exists(pathToLogFile))
            {
                using (var loggerFile = File.Create(pathToLogFile))
                {
                    //Do nothing
                }               
            }
            pathFile =  pathToLogFile;
            using (StreamWriter writer = new StreamWriter(pathFile))
            {
                writer.WriteLine($"Logger created at time: {DateTime.Now}. Starting to await for logs... ");
            }
        }
        public void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            using (StreamWriter writer = new StreamWriter(pathFile, true))
            {
                writer.WriteLine($"Time: {DateTime.Now}: {message}");
            }
        }
    }
}
