using Crestron.SimplSharp.CrestronIO;
using System;

namespace ATC.Framework.Nexus
{
    internal class ApiRequest : IDisposable
    {
        private readonly string content;

        public HttpMethod Method { get; private set; }
        public string Url { get; private set; }
        public Stream Content
        {
            get
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(content);
                writer.Flush();
                stream.Position = 0;
                return stream;
            }
        }

        public ApiRequest(HttpMethod method, string url, string content)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL is null or empty.");
            else if (content == null)
                throw new ArgumentNullException("content");

            Method = method;
            Url = url;
            this.content = content;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}, Content length: {2} bytes", Method.ToString().ToUpper(), Url, Content.Length);
        }

        public void Dispose()
        {
            Content.Dispose();
        }

        public Crestron.SimplSharp.Net.Https.RequestType GetHttpsRequestType()
        {
            return (Crestron.SimplSharp.Net.Https.RequestType)Method;
        }

        public Crestron.SimplSharp.Net.Http.RequestType GetHttpRequestType()
        {
            return (Crestron.SimplSharp.Net.Http.RequestType)Method;
        }
    }
}
