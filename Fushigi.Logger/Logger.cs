using System;

namespace Fushigi.Logger
{
    public static class Logger
    {
        public static bool IsInitialized { get; private set; }

        private static FileStream? mOutputStream;
        private static StreamWriter? mConsoleWriter;

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

        private static void LogLine(object msg)
        {
            if (!IsInitialized || mConsoleWriter == null) return;

            mConsoleWriter.WriteLine(msg);
            Console.WriteLine(msg);
        }

        private static void LogWrite(object msg)
        {
            if (!IsInitialized || mConsoleWriter == null) return;

            mConsoleWriter.Write(msg);
            Console.Write(msg);
        }

        public static void LogMessage(object from, object msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            LogWrite($"[{from}]: ");

            Console.ForegroundColor = ConsoleColor.White;
            LogLine($"{msg}");
        }

        public static void LogWarning(object from, object msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            LogLine($"[{from}]: {msg}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogError(object from, object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            LogLine($"[{from}]: {msg}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogError(Exception e)
        {
            if (e.StackTrace == null)
                LogLine(e.Message);
            else
                LogLine($"[ERROR] {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
        }
    }
}
