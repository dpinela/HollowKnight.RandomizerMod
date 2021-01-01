// ReSharper disable file UnusedMember.Global

namespace RandomizerLib.Logging
{
    public static class LogHelper
    {
        public static ILogger LogTarget = new ConsoleLogger();

        public static void Log(string message)
        {
            LogTarget.Log(message);
        }

        public static void Log(object message)
        {
            LogTarget.Log(message);
        }

        public static void LogDebug(string message)
        {
            LogTarget.LogDebug(message);
        }

        public static void LogDebug(object message)
        {
            LogTarget.LogDebug(message);
        }

        public static void LogError(string message)
        {
            LogTarget.LogError(message);
        }

        public static void LogError(object message)
        {
            LogTarget.LogError(message);
        }

        public static void LogFine(string message)
        {
            LogTarget.LogFine(message);
        }

        public static void LogFine(object message)
        {
            LogTarget.LogFine(message);
        }

        public static void LogWarn(string message)
        {
            LogTarget.LogWarn(message);
        }

        public static void LogWarn(object message)
        {
            LogTarget.LogWarn(message);
        }
    }
}
