using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TrackService.Helper
{
    public class WriteLog
    {
        public static void WriteLogToFile(string message)
        {
            string directory = System.AppDomain.CurrentDomain.BaseDirectory + "Logs";
            if (!Directory.Exists(directory))
            {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(directory);
            }

            var logFile = Path.Combine(directory, "log.txt");

            using (StreamWriter sw = File.AppendText(logFile))
            {
                sw.WriteLineAsync(DateTime.Now.ToString() + ": " + message);
            }
        }
    }
}
