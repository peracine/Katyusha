using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Katyusha
{
    public class KLog
    {
        private enum FileType
        { 
            Info,
            Error,
            Report
        }

        /// <summary>
        /// Save the error in the 'Personal' folder.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="correlation"></param>
        /// <returns></returns>
        public static async Task Error(Exception exception, string correlation = null)
        {
            using (var streamWriter = new StreamWriter(GetLogFile(FileType.Error), true))
            {
                await streamWriter.WriteLineAsync($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}: {correlation} : {exception.Message}");
            }
        }

        /// <summary>
        /// Save the report in the 'Personal' folder.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static async Task Report(string text)
        {
            using (var streamWriter = new StreamWriter(GetLogFile(FileType.Report), true))
            {
                await streamWriter.WriteLineAsync(text);
            }
        }

        /// <summary>
        /// Serialize the KResponses in the 'Personal' folder.
        /// </summary>
        /// <param name="kResponse"></param>
        /// <param name="includeContent"></param>
        /// <returns></returns>
        public static async Task Report(KResponse kResponse, bool includeContent = false)
        {
            using (var streamWriter = new StreamWriter(GetLogFile(FileType.Report), true))
            {
                string correlationId = string.IsNullOrEmpty(kResponse.CorrelationId) ? string.Empty : kResponse.CorrelationId;
                string content = string.Empty;
                if (kResponse.Response != null && includeContent)
                {
                    content = await kResponse.Response.Content.ReadAsStringAsync();
                    content = "," + Regex.Replace(content, @"\t|\n|\r", string.Empty);
                }

                string response = kResponse.Response == null ? "NULL" : $"{kResponse.Response.StatusCode.ToString()},{kResponse.Response.RequestMessage.Method.Method},{kResponse.Response.RequestMessage.RequestUri}{content}";
                await streamWriter.WriteLineAsync($"{kResponse.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")},{kResponse.Id},{correlationId},{kResponse.ElapsedTime},{response}");
            }
        }

        private static string GetLogFile(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Error:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"KError_{DateTime.Today.ToString("yyyy-MM-dd")}.log");

                case FileType.Report:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"KReport_{DateTime.Today.ToString("yyyy-MM-dd")}.log");

                default:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"KInfo_{DateTime.Today.ToString("yyyy-MM-dd")}.log");
            }
        }
    }
}
