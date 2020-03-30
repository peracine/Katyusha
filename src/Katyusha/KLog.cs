using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Katyusha
{
    public static class KLog
    {
        private enum FileType
        {
            Info,
            Error,
            Report
        }

        /// <summary>
        /// Logs a message in the 'Personal' folder (KInfo_*.log file).
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Info(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            using var streamWriter = new StreamWriter(GetLogFile(FileType.Info), true);
            await streamWriter.WriteLineAsync(message);
        }

        /// <summary>
        /// Logs the serialized KResponse in the 'Personal' folder (KReport_*.log file).
        /// </summary>
        /// <param name="kResponses">Result returned by the method KClient.Send().</param>
        /// <param name="reportFile">Optional. Absolute path to the report.</param>
        /// <returns></returns>
        public static async Task ReportAsync(KResponse[] kResponses, string reportFile = null)
        {
            if (kResponses == null || !kResponses.Any())
                return;

            if (string.IsNullOrEmpty(reportFile))
            {
                reportFile = GetLogFile(FileType.Report);
            }

            var serializedResponses = new List<Task<string>>();
            foreach (var kResponse in kResponses.OrderBy(kr => kr.Timestamp))
            {
                serializedResponses.Add(GetSerializedResponse(kResponse));
            }

            using var streamWriter = new StreamWriter(reportFile, true);
            foreach (string log in await Task.WhenAll(serializedResponses).ConfigureAwait(false))
            {
                streamWriter.WriteLine(log);
            }
        }

        private static async Task<string> GetSerializedResponse(KResponse kResponse)
        {
            string content;
            if (kResponse.Exception == null && kResponse.Response != null && kResponse.Response.Content != null)
            {
                content = await kResponse.Response.Content.ReadAsStringAsync();
                content = "," + Regex.Replace(content, @"\t|\n|\r", string.Empty);
            }
            else if (kResponse.Exception != null)
            {
                if (kResponse.Exception.GetType() == typeof(OperationCanceledException))
                {
                    content = ",Exception:Timeout";
                }
                else
                {
                    content = $",Exception:{kResponse.Exception.GetType()}:{kResponse.Exception.Message}:{kResponse.Exception.Source}";
                }
            }
            else
            {
                content = string.Empty;
            }

            string log = kResponse.Response == null ? "NULL" : $"{kResponse.Response.StatusCode},{kResponse.Response.RequestMessage.Method.Method},{kResponse.Response.RequestMessage.RequestUri}{content}";
            return $"{kResponse.Timestamp:yyyy-MM-dd HH:mm:ss.ffff},{kResponse.ElapsedTime},{log}";
        }

        /// <summary>
        /// Logs the error in the 'Personal' folder (KError_*.log file).
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static async Task Error(Exception exception)
        {
            if (exception == null)
                return;

            using var streamWriter = new StreamWriter(GetLogFile(FileType.Error), true);
            await streamWriter.WriteLineAsync($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {exception.Message}");
        }

        private static string GetLogFile(FileType fileType) =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"K{fileType}_{DateTime.Today:yyyy-MM-dd}.log");
    }
}