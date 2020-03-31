using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Katyusha
{
    public static class KConsoleLog
    {
        /// <summary>
        /// Print out basic statistics in 'Debug' mode.
        /// </summary>
        /// <param name="kResponses"></param>
        /// <param name="uri">Optional.</param>
        /// <param name="httpMethod">Optional.</param>
        /// <param name="intervalInMilliseconds">Optional.</param>
        public static void Print(KResponse[] kResponses, string uri = "", HttpMethod httpMethod = null, long intervalInMilliseconds = 1000)
        {
            if (kResponses == null || !kResponses.Any())
                return;

            Debug.WriteLine(new string('#', 100));
            Debug.WriteLine($"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            
            if (!string.IsNullOrEmpty(uri) )
                Debug.WriteLine($"Uri: {uri}");

            if (httpMethod != null)
                Debug.WriteLine($"Method: {httpMethod.ToString().ToUpper()}");

            Debug.WriteLine($"Requests: {kResponses.Count()}");
            Debug.WriteLine(string.Empty);
            Debug.WriteLine("STATUS CODES");
            foreach (var httpStatusCode in kResponses.Where(kr => kr.Response != null).Select(kr => kr.Response.StatusCode).Distinct().OrderBy(s => (int)s))
            {
                Debug.WriteLine($"{httpStatusCode}: {kResponses.Where(kr => kr.Response != null).Count(kr => kr.Response.StatusCode == httpStatusCode)}");
            }

            int totalExceptions = kResponses.Count(kr => kr.Exception != null);
            if (totalExceptions > 0)
                Debug.WriteLine($"Exceptions: {totalExceptions}");

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("RESPONSE TIMES");
            long min = kResponses.Select(kr => kr.ElapsedTime).Min();
            long max = kResponses.Select(kr => kr.ElapsedTime).Max();
            Debug.WriteLine($"min: {min}ms   max: {max}ms");
            Debug.WriteLine(string.Empty);
            long intervalBegin = 0;
            long intervalEnd = intervalInMilliseconds;

            while (intervalBegin < max)
            {
                Debug.WriteLine($"{intervalBegin} <= t < {intervalEnd} : {kResponses.Count(kr => kr.ElapsedTime >= intervalBegin && kr.ElapsedTime < intervalEnd)}");
                intervalBegin = intervalEnd;
                intervalEnd += intervalInMilliseconds;
            }
            Debug.WriteLine(new string('#', 100));
            Debug.WriteLine(string.Empty);
        }
     }
}