using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Katyusha
{
    public class KClient
    {
        /// <summary>
        /// Timeout expressed in seconds.
        /// </summary>
        public uint Timeout { get; set; } = 30;
        /// <summary>
        /// Stores the response body in the result.
        /// </summary>
        public bool IncludeResponseBodyInResult { get; set; } = true;
        /// <summary>
        /// Total requests send in 1 second.
        /// </summary>
        public uint RequestsPerSecond { get; }
        /// <summary>
        /// Amount of requests send at the same time.
        /// </summary>
        public uint BatchSize { get; }
        /// <summary>
        /// Total repetitions to perform. This is equivalent to a duration in seconds.
        /// </summary>
        public uint Repetition { get; }
        private static HttpClient _client;

        /// <summary>
        /// Creates a http client and prepares all requests.
        /// </summary>
        /// <param name="requestsPerSecond">Optional. RPS.</param>
        /// <param name="batchSize">Optional. Cannot be greater than requestsPerSecond.</param>
        /// <param name="repetition">Optional. Total repetitions to perform.</param>
        public KClient(uint requestsPerSecond = 1, uint batchSize = 1, uint repetition = 1)
        {
            RequestsPerSecond = requestsPerSecond;
            BatchSize = batchSize >= requestsPerSecond ? requestsPerSecond : batchSize;
            Repetition = repetition;
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(Timeout)
            };
        }

        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="request">Valid KRequest object.</param>
        /// <returns></returns>
        public async Task<KResponse[]> SendAsync(KRequest request)
        {
            var requests = new List<Task<KResponse>>();
            int numberOfBatchesPerSecond = (int)Math.Ceiling(Convert.ToDouble(RequestsPerSecond) / Convert.ToDouble(BatchSize));
            int interval = (int)Math.Floor(Convert.ToDouble(1000 / numberOfBatchesPerSecond));

            int totalRequestsInTheCurrentBatch = 0;
            int totalRequestsInTheCurrentInterval = 0;
            int requestId = 0;
            int delay = 0;

            while (requestId < RequestsPerSecond * Repetition)
            {
                requests.Add(SendWithDelayAsync(
                    delay: delay,
                    request: MapToHttpRequestMessage(request),
                    includeResponseBodyInResult: IncludeResponseBodyInResult));

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

        private async Task<KResponse> SendWithDelayAsync(int delay, HttpRequestMessage request, bool includeResponseBodyInResult)
        {
            await Task.Delay(delay).ConfigureAwait(false);
            var timestamp = DateTime.UtcNow;
            try
            {
                var completionOption = includeResponseBodyInResult ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;
                var stopWatch = Stopwatch.StartNew();
                var response = await _client.SendAsync(request, completionOption).ConfigureAwait(false);
                return new KResponse(timestamp, stopWatch.ElapsedMilliseconds, response, includeResponseBodyInResult);
            }
            catch (Exception exception)
            {
                long elapsedTime = exception.GetType() == typeof(OperationCanceledException) ? Timeout * 1000 : 0;
                return new KResponse(timestamp, elapsedTime, null, false, exception);
            }
        }

        private static HttpRequestMessage MapToHttpRequestMessage(KRequest kRequest)
        {
            var httpRequestMessage = new HttpRequestMessage(kRequest.Method, kRequest.Endpoint)
            {
                Content = kRequest.Content
            };

            foreach (var header in kRequest.Headers)
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }

            return httpRequestMessage;
        }
    }
}