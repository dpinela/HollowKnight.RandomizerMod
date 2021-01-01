using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerLib.Logging
{
    internal class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine("[INFO] " + message);
        }

        public void Log(object message)
        {
            Console.WriteLine("[INFO] " + message);
        }

        public void LogDebug(string message)
        {
            Console.WriteLine("[DEBUG] " + message);
        }

        public void LogDebug(object message)
        {
            Console.WriteLine("[DEBUG] " + message);
        }

        public void LogError(string message)
        {
            Console.WriteLine("[ERROR] " + message);
        }

        public void LogError(object message)
        {
            Console.WriteLine("[ERROR] " + message);
        }

        public void LogFine(string message)
        {
            Console.WriteLine("[FINE] " + message);
        }

        public void LogFine(object message)
        {
            Console.WriteLine("[FINE] " + message);
        }

        public void LogWarn(string message)
        {
            Console.WriteLine("[WARN] " + message);
        }

        public void LogWarn(object message)
        {
            Console.WriteLine("[WARN] " + message);
        }
    }
}
