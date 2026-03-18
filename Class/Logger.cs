using System;
using System.IO;
using GTA.Math;

namespace PDMCD4
{
    public static class Logger
    {
        public static bool Enabled => Helper.optLogging;

        public static void Log(object message)
        {
            if (Enabled)
            {
                File.AppendAllText(@".\PDM.log", $"{DateTime.Now}:{message}{Environment.NewLine}");
            }
        }

        public static void PinPoint(Vector3 message)
        {
            File.AppendAllText(
                @".\PinPoint.log",
                string.Format(
                    "{0}:{1},{2},{3}{4}",
                    DateTime.Now,
                    message.X,
                    message.Y,
                    message.Z,
                    Environment.NewLine));
        }
    }

    public static class logger
    {
        public static void Log(object message) => Logger.Log(message);
        public static void PinPoint(Vector3 message) => Logger.PinPoint(message);
    }
}
