using System;

namespace Fushigi.Logger
{
    public static class Logger
    {
        public static bool IsInitialized { get; private set; }

        private static FileStream? mOutputStream;
        private static StreamWriter? mConsoleWriter;
        public static bool IsDebugMode = false;

        public static void CreateLogger()
        {
            if (IsInitialized) return;
            
            mOutputStream = new FileStream("output.log", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            mConsoleWriter = new StreamWriter(mOutputStream)
            {
                AutoFlush = true
            };

            IsInitialized = true;

            LogMessage("Logger", "Initialized logger");
        }

        public static void CloseLogger()
        {
            if (!IsInitialized || mOutputStream == null || mConsoleWriter == null)
            {
                LogError("Logger", "Can't close logger before initializing!");
                return;
            }

            mOutputStream.Close();

            IsInitialized = false;
        }

        public static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!IsInitialized || e.ExceptionObject == null) return;
            if (e.ExceptionObject is not Exception exception) return;

            Console.ForegroundColor = ConsoleColor.Red;
            LogError(exception);
            Console.ForegroundColor = ConsoleColor.White;

            Environment.Exit(1);
        }

        private static void Log(object msg)
        {
            if (!IsInitialized || mConsoleWriter == null) return;

            mConsoleWriter.WriteLine(msg);
            Console.WriteLine(msg);
        }

        public static void LogMessage(object from, object msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Log($"[{from}]: {msg}");
        }
        // Probably helpful
        // Only prints if startet with run arguments -d true if -d is not set or just not true it will not print
        // also has a check if has already been printed in the last 10 minutes OR if it has been 5 seconds since the last time
        // it has been tried to print
        private readonly record struct DebugLogCacheEntry(string from, string msg);

        private struct DebugLogCacheValue
        {
            public long first;
            public long last;
        }
        private static Dictionary<DebugLogCacheEntry,DebugLogCacheValue> debugLogCache = new Dictionary<DebugLogCacheEntry, DebugLogCacheValue>();
        public static void LogDebug(object from, object msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (IsDebugMode)
            {
                DebugLogCacheEntry entry = new DebugLogCacheEntry { from = $"{from}", msg = $"{msg}" };
                bool shouldLog = false;
                if (debugLogCache.TryGetValue(entry, out DebugLogCacheValue val))
                {
                    if (val.first < now - 600 || val.last < now-5)
                    {
                        shouldLog = true;
                        val.last = now;
                    }
                    debugLogCache[entry] = val;
                }
                else
                {
                    shouldLog = true;
                    debugLogCache.Add(entry, new DebugLogCacheValue{first =  now, last = now});
                }

                if (shouldLog)
                {
                    Log($"\x1b[36m {{DEBUG}} \x1b[0m[{from}]: {msg}");
                }
            }
        }

        public static void LogWarning(object from, object msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Log($"[WARN] [{from}]: {msg}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogError(object from, object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log($"[ERROR] [{from}]: {msg}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogError(Exception e)
        {
            if (e.StackTrace == null)
                Log(e.Message);
            else
                Log($"[ERROR] {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
        }
    }
}
