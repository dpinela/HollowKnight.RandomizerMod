using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modding;

namespace RandomizerLib.Logging
{
    public class ModLogger : ILogger
    {
        private Mod mod;

        public ModLogger(Mod mod)
        {
            this.mod = mod;
        }
        public void Log(string message)
        {
            mod.Log(message);
        }

        public void Log(object message)
        {
            mod.Log(message);
        }

        public void LogDebug(string message)
        {
            mod.LogDebug(message);
        }

        public void LogDebug(object message)
        {
            mod.LogDebug(message);
        }

        public void LogError(string message)
        {
            mod.LogError(message);
        }

        public void LogError(object message)
        {
            mod.LogError(message);
        }

        public void LogFine(string message)
        {
            mod.LogFine(message);
        }

        public void LogFine(object message)
        {
            mod.LogFine(message);
        }

        public void LogWarn(string message)
        {
            mod.LogWarn(message);
        }

        public void LogWarn(object message)
        {
            mod.LogWarn(message);
        }
    }
}
