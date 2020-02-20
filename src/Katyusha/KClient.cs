using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Katyusha
{
    public class KClient
    {
        /// <summary>
        /// Timeout expressed in seconds
        /// </summary>
        public uint Timeout { get; set; } = 30;
        public uint RequestsPerSecond { get; }
        /// <summary>
        /// Amount of requests send at the same time
        /// </summary>
        public uint BatchSize { get; }
        /// <summary>
        /// Test duration expressed in seconds. Keep this value low!
        /// </summary>
        public uint Duration { get; }
        private static HttpClient _client;

        /// <summary>
        /// Create a http client and prepare all requests. The requests will be send one by one if batchSize = 1.
        /// </summary>
        /// <param name="requestsPerSecond"></param>
        /// <param name="batchSize"></param>
        public KClient(uint requestsPerSecond = 1, uint batchSize = 1, uint duration = 1)
        {
            RequestsPerSecond = requestsPerSecond;
            BatchSize = batchSize >= requestsPerSecond ? requestsPerSecond : batchSize;
            Duration = duration;
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(Timeout);
        }

        /// <summary>
        /// Send all requests. correlationId is used for reporting purpose.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public async Task<KResponse[]> SendAsync(KRequest request, string correlationId = null)
        {
            var requests = new List<Task<KResponse>>();
            int numberOfBatchesPerSecond = (int)Math.Ceiling(Convert.ToDouble(RequestsPerSecond) / Convert.ToDouble(BatchSize));
            int interval = (int)Math.Floor(Convert.ToDouble(1000 / numberOfBatchesPerSecond));

            int totalRequestsInTheCurrentBatch = 0;
            int totalRequestsInTheCurrentInterval = 0;
            int requestId = 0;
            int delay = 0;

            while (requestId < RequestsPerSecond * Duration)
            {
                requests.Add(SendWithDelayAsync(
                    delay: delay,
                    request: request,
                    correlationId: correlationId));

                totalRequestsInTheCurrentBatch++;
                totalRequestsInTheCurrentInterval++;

                if (totalRequestsInTheCurrentBatch >= BatchSize)
                {
                    delay += interval;
                    totalRequestsInTheCurrentBatch = 0;
                }

                if (totalRequestsInTheCurrentInterval >= RequestsPerSecond)
                {
                    delay += interval;
                    totalRequestsInTheCurrentBatch = 0;
                    totalRequestsInTheCurrentInterval = 0;
                }

                requestId++;
            }

            return await Task.WhenAll(requests).ConfigureAwait(false);
        }

        private async Task<KResponse> SendWithDelayAsync(int delay, KRequest request, string correlationId = null)
        {
            await Task.Delay(delay).ConfigureAwait(false);
            var timestamp = DateTime.UtcNow;
            try
            {
                var httpRequestMessage = new HttpRequestMessage(request.Method, request.Endpoint);
                httpRequestMessage.Content = request.Content;
                if (request.Headers != null && request.Headers.Any())
                {
                    foreach (var header in request.Headers)
                    {
                        httpRequestMessage.Headers.Add(header.Key, header.Value);
                    }
                }

                var stopWatch = Stopwatch.StartNew();
                var response = await _client.SendAsync(httpRequestMessage).ConfigureAwait(false);
                return new KResponse(timestamp, stopWatch.ElapsedMilliseconds, response, correlationId);

            }
            catch (OperationCanceledException) //Timeout exception
            {
                return new KResponse(timestamp, Timeout * 1000, null, correlationId);
            }
            catch (Exception) //Other exception
            {
                return new KResponse(timestamp, 0, null, correlationId);
            }
        }
    }
}
