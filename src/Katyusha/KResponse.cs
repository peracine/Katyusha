using System;
using System.Net.Http;

namespace Katyusha
{
    public class KResponse
    {
        public Guid Id { get; }
        /// <summary>
        /// UTC time
        /// </summary>
        public DateTime Timestamp { get; }
        /// <summary>
        /// Elapsed time in milliseconds
        /// </summary>
        public long ElapsedTime { get; }
        public HttpResponseMessage Response { get; }
        public string CorrelationId { get; }

        private KResponse()
        {
        }

        public KResponse(DateTime timestamp, long elapsedTime, HttpResponseMessage response, string correlationId = null)
        {
            Id = Guid.NewGuid();
            Timestamp = timestamp;
            ElapsedTime = elapsedTime;
            Response = response;
            CorrelationId = correlationId;
        }
    }
}
