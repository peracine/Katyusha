using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
            var client = new KClient(3, 2);

            var results = await client.SendAsync(request);

            Assert.True(results.Count() == client.RequestsPerSecond);
            Assert.Empty(results.Where(r => r.Response.StatusCode != HttpStatusCode.OK));
            Assert.Empty(results.Where(r => r.ElapsedTime > 1000));
        }

        [Fact]
        public async Task Send_GetWithHeader_returns_200()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", $"Bearer xyz");
            var request = new KRequest(HttpMethod.Get, new Uri(_endpoint), headers);
            var client = new KClient();

            var results = await client.SendAsync(request);

            Assert.True(results.First().Response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task Send_GetWithReport_returns_true()
        {
            var request = new KRequest(HttpMethod.Get, new Uri(_endpoint));
            var client = new KClient();
            string correlationId = Guid.NewGuid().ToString();
            string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"KReport_{DateTime.Today.ToString("yyyy-MM-dd")}.log");

            var results = await client.SendAsync(request, correlationId);

            foreach (var result in results)
                await KLog.Report(result, true);

            Assert.True(File.Exists(reportPath));
        }

        [Fact]
        public async Task Send_PostContent_returns_201()
        {
            var request = new KRequest(HttpMethod.Post, new Uri(_endpoint));
            request.SetContent(new StringContent(JsonSerializer.Serialize(new { Id = 1, Name = "Test" }), Encoding.UTF8, "application/json"));
            var client = new KClient();

            var results = await client.SendAsync(request);

            Assert.True(results.First().Response.StatusCode == HttpStatusCode.Created);
        }

        [Fact]
        public async Task Send_PostMultipartContent_returns_201()
        {
            var stream = new MemoryStream(GetTestTextFileAsByteArray()) as Stream;
            var request = new KRequest(HttpMethod.Post, new Uri(_endpoint));
            request.SetMultipartContent(new List<Stream>() { stream });
            var client = new KClient();

            var results = await client.SendAsync(request);

            Assert.True(results.First().Response.StatusCode == HttpStatusCode.Created);
        }

        private byte[] GetTestTextFileAsByteArray()
        {
            using (var memoryStream = new MemoryStream())
            using (var tw = new StreamWriter(memoryStream))
            {
                tw.WriteLine("Test file");
                tw.Flush();
                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
        }
    }
}
