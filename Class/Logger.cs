using System;
using System.IO;
using GTA.Math;

namespace PremiumDeluxeRevamped
{
    public static class Logger
    {
        private static readonly object SyncRoot = new object();
        private static bool _sessionPrepared;

        public static bool Enabled => Helper.optLogging;

        private static string LogDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PremiumDeluxeRevamped");
        private static string LogFilePath => Path.Combine(LogDirectory, "PDM.log");
        private static string PinPointFilePath => Path.Combine(LogDirectory, "PinPoint.log");

        public static void Log(object message)
        {
            if (!Enabled)
            {
                return;
            }

            AppendLine(LogFilePath, message);
        }

        public static void PinPoint(Vector3 message)
        {
            AppendLine(PinPointFilePath, $"{message.X},{message.Y},{message.Z}");
        }

        private static void AppendLine(string filePath, object message)
        {
            try
            {
                lock (SyncRoot)
                {
                    PrepareSessionFiles();
                    File.AppendAllText(filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}{Environment.NewLine}");
                }
            }
            catch
            {
            }
        }

        private static void PrepareSessionFiles()
        {
            if (_sessionPrepared)
            {
                return;
            }

            Directory.CreateDirectory(LogDirectory);

            try { File.WriteAllText(LogFilePath, string.Empty); } catch { }
            try { File.WriteAllText(PinPointFilePath, string.Empty); } catch { }

            _sessionPrepared = true;
        }
    }

    public static class logger
    {
        public static void Log(object message) => Logger.Log(message);
        public static void PinPoint(Vector3 message) => Logger.PinPoint(message);
    }
}
