using Crestron.SimplSharp.Net.Http;
using System;
using System.Text;

namespace ATC.Framework.Nexus
{
    internal class HttpRequestManager : RequestManagerBase
    {
        private readonly HttpClient httpClient = new HttpClient()
        {
            Accept = "*/*",
            KeepAlive = false,
            Timeout = 10,
            TimeoutEnabled = true,
            UserAgent = "NexusSystemAgent",
        };

        public HttpRequestManager(string companyId, string groupId, string systemId, string secret, string apiUrl)
            : base(companyId, groupId, systemId, secret, apiUrl) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient.Dispose();
            }
        }

        protected override void SendNextRequest()
        {
            // check that client isn't already busy
            if (httpClient.ProcessBusy)
            {
                TraceError("SendNextRequest() called but client is busy.");
                return;
            }

            // get first request from queue
            ApiRequest apiRequest = requestQueue.Peek();
            Trace("SendNextRequest() sending first item in queue, count: " + requestQueue.Count);

            try
            {
                // create new request object
                var clientRequest = new HttpClientRequest()
                {
                    RequestType = apiRequest.GetHttpRequestType(),
                    Url = new UrlParser(apiRequest.Url),
                    Encoding = Encoding.UTF8,
                    ContentStream = apiRequest.Content,
                };

                // set request headers
                clientRequest.Header.ContentType = "application/json";
                clientRequest.Header.SetHeaderValue("Accept", "*/*");
                clientRequest.Header.SetHeaderValue("Secret", Secret);
                clientRequest.Header.SetHeaderValue("Content-Length", apiRequest.Content.Length.ToString());
                clientRequest.Header.SetHeaderValue("Expect", string.Empty); // remove Expect: 100-continue header which was causing 100 status code response

                // dispatch request asynchronously
                var result = httpClient.DispatchAsync(clientRequest, ResponseCallback);
                if (result == HttpClient.DISPATCHASYNC_ERROR.PENDING)
                    Trace("SendNextRequest() successfully dispatched request: " + apiRequest.ToString());
                else
                {
                    TraceError("SendNextRequest() error dispatching request: " + apiRequest.ToString());
                    AdvanceQueue();
                }
            }
            catch (Exception ex)
            {
                TraceException("SendNextRequest() exception caught.", ex);
            }
        }

        private void ResponseCallback(HttpClientResponse response, HTTP_CALLBACK_ERROR error)
        {
            try
            {
                if (error == HTTP_CALLBACK_ERROR.COMPLETED)
                {
                    string message = string.Format("ResponseCallback() response code: {0}", response.Code);
                    string contentString = response.ContentString;
                    if (contentString != string.Empty)
                        message += string.Format(", \"{0}\"", contentString);

                    if (response.Code >= 400)
                        TraceWarning(message);
                    else
                        Trace(message);
                }
                else
                {
                    TraceError(string.Format("ResponseCallback() error receiving response. Error: {0}", error));
                }
            }
            catch (Exception ex)
            {
                TraceException("ResponseCallback() exception caught.", ex);
            }

            AdvanceQueue();
        }
    }
}
