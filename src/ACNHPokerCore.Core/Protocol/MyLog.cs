using System;
using System.IO;

namespace ACNHPokerCore.Core
{
    /// <summary>Ported from Custom/MyLog.cs. Only change: uses Path.Combine instead of a
    /// hardcoded "\\" separator so the log file lands in the right place on Linux too.</summary>
    public class MyLog
    {
        public static void LogEvent(string Location, string Message)
        {
            if (!File.Exists(Utilities.logPath))
            {
                string logheader = "Timestamp" + "," + "Form" + "," + "Message";

                string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), Utilities.saveFolder);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using StreamWriter sw = File.CreateText(Utilities.logPath);
                sw.WriteLine(logheader);
            }

            DateTime localDate = DateTime.Now;

            string newLog = localDate + "," + Location + "," + Message;

            if (File.Exists(Utilities.logPath))
            {
                using StreamWriter sw = File.AppendText(Utilities.logPath);
                sw.WriteLine(newLog);
            }
        }
    }
}
