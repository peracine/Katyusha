using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Katyusha.Test
{
    public class KClientTest
    {
        const string _endpoint = "https://reqres.in/api/users";

        [Fact]
        public async Task Send_Get_returns_200()
        {
            var request = new KRequest(HttpMethod.Get, new Uri(_endpoint));
            var client = new KClient(3, 2, 2); //First batch with 2 requests and a second 500ms later with 1 request, with two iterations (=> total 6 requests)

            var results = await client.SendAsync(request);

            Assert.True(results.Count() == client.RequestsPerSecond * client.Repetition);
            Assert.Empty(results.Where(r => r.Response.StatusCode != HttpStatusCode.OK));
            Assert.Empty(results.Where(r => r.ElapsedTime > 1000));
        }

        [Fact]
        public async Task Send_GetWithHeader_returns_200()
        {
            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer xyz" }
            };
            var request = new KRequest(HttpMethod.Get, new Uri(_endpoint), headers);
            var client = new KClient();

            var results = await client.SendAsync(request);

            Assert.True(results.First().Response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task Send_GetWithReportWithResponseBody_returns_true()
        {
            var request = new KRequest(HttpMethod.Get, new Uri(_endpoint));
            var client = new KClient();
            string reportFile = Path.Combine(Directory.GetCurrentDirectory(), $"KReport_{DateTime.Today:yyyy-MM-dd}__{Guid.NewGuid()}.csv");
            
            var results = await client.SendAsync(request);
            await KLog.ReportAsync(results, reportFile);

            Assert.True(File.Exists(reportFile));
        }

        [Fact]
        public async Task Send_GetWithReportWithoutResponseBody_returns_true()
        {
            var request = new KRequest(HttpMethod.Get, new Uri(_endpoint));
            var client = new KClient()
            { 
                IncludeResponseBodyInResult = false
            };
            string reportFile = Path.Combine(Directory.GetCurrentDirectory(), $"KReport_{DateTime.Today:yyyy-MM-dd}__{Guid.NewGuid()}.csv");
            
            var results = await client.SendAsync(request);
            await KLog.ReportAsync(results, reportFile);

            Assert.DoesNotContain(results, r => r.Response.Content != null);
            Assert.True(File.Exists(reportFile));
        }

        [Fact]
        public async Task Send_PostWithObject_returns_201()
        {
            var request = new KRequest(HttpMethod.Post, new Uri(_endpoint));
            request.SetContent(new { Id = 1, Name = "Test" });
            var client = new KClient();

            var results = await client.SendAsync(request);

            Assert.True(results.First().Response.StatusCode == HttpStatusCode.Created);
        }

        [Fact]
        public async Task Send_PostWithByteArrays_returns_201()
        {
            var request = new KRequest(HttpMethod.Post, new Uri(_endpoint));
            request.SetContent(new List<byte[]>() { GetTestTextFileAsByteArray(), GetTestTextFileAsByteArray() });
            var client = new KClient();

            var results = await client.SendAsync(request);

            Assert.True(results.First().Response.StatusCode == HttpStatusCode.Created);
        }

        private byte[] GetTestTextFileAsByteArray()
        {
            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream);
            streamWriter.WriteLine($"Test file generated {DateTime.Now.ToLongDateString()}.");
            streamWriter.Flush();
            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }
    }
}
