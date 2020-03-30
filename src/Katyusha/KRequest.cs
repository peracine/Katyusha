using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Katyusha
{
    public class KRequest
    {
        public HttpMethod Method { get; }
        public Uri Endpoint { get; }
        public Dictionary<string, string> Headers { get; }
        public HttpContent Content { get; private set; }

        private KRequest()
        {
        }

        /// <summary>
        /// Standard request without body.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="endpoint"></param>
        /// <param name="headers"></param>
        public KRequest(HttpMethod method, Uri endpoint, Dictionary<string, string> headers = null)
        {
            Method = method ?? throw new ArgumentNullException($"The method parameter cannot be null.");
            Endpoint = endpoint.IsWellFormedOriginalString() ? endpoint : throw new ArgumentException($"The endpoint parameter ({endpoint}) is invalid.");
            Headers = headers ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Sets the request body. Generic method.
        /// </summary>
        /// <param name="content"></param>
        public void SetContent(HttpContent content)
        {
            Content = content;
        }

        /// <summary>
        /// Sets the request body. Sends the content as a json object.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public void SetContent(object content)
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        /// <summary>
        /// Sets the request body. Sends the content as a multipart form (uploading files).
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public void SetContent(IEnumerable<byte[]> files)
        {
            if (files == null || !files.Any())
                return;

            var multipartForm = new MultipartFormDataContent("----------");
            multipartForm.Headers.ContentType.MediaType = "multipart/form-data";
            int index = 0;
            foreach (var file in files.Where(f => f != null))
            {
                multipartForm.Add(new ByteArrayContent(file), "files", $"File{index}");
                index++;
            }

            Content = multipartForm;
        }
    }
}