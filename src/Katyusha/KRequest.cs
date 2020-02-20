using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

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
            Endpoint = endpoint.IsWellFormedOriginalString() ? endpoint : throw new ArgumentException($"The endpoint parameter ({endpoint.ToString()}) is invalid.");
            Headers = headers;
        }

        /// <summary>
        /// Set the request body.
        /// </summary>
        /// <param name="content"></param>
        public void SetContent(StringContent content)
        {
            Content = content;
        }

        /// <summary>
        /// Set the request body for a multipart/form-data request (file uploading f.ex.).
        /// </summary>
        /// <param name="files"></param>
        public void SetMultipartContent(IEnumerable<Stream> files)
        {
            if (files == null || !files.Any())
                throw new ArgumentNullException($"The files parameter cannot be null or empty.");
 
            var multipartForm = new MultipartFormDataContent("KTEST");
            multipartForm.Headers.ContentType.MediaType = "multipart/form-data";
            int index = 0;
            foreach (var file in files)
            {
                multipartForm.Add(new StreamContent(file), $"file_{index}", $"File{index}");
                index++;
            }

            Content = multipartForm;
        }
    }
}
