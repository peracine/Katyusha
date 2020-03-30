using System;
using System.Net.Http;

namespace Katyusha
{
    public class KResponse
    {
        /// <summary>
        /// Time in UTC.
        /// </summary>
        public DateTime Timestamp { get; }
        /// <summary>
        /// Elapsed time in milliseconds.
        /// </summary>
        public long ElapsedTime { get; }
        public HttpResponseMessage Response { get; }
        public Exception Exception { get; }

        private KResponse()
        {
        }

        public KResponse(DateTime timestamp, long elapsedTime, HttpResponseMessage response, bool includeResponseBodyInResult, Exception exception = null)
        {
            if (response != null && !includeResponseBodyInResult)
                response.Content = null;

            Timestamp = timestamp;
            ElapsedTime = elapsedTime;
            Response = response;
            Exception = exception;
        }
    }
}
