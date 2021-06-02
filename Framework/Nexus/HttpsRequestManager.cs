using Crestron.SimplSharp.Net;
using Crestron.SimplSharp.Net.Https;
using System;
using System.Text;

namespace ATC.Framework.Nexus
{
    internal class HttpsRequestManager : RequestManagerBase
    {
        private readonly HttpsClient httpsClient = new HttpsClient()
        {
            Accept = "*/*",
            AuthenticationMethod = AuthMethod.NONE,
            HostVerification = false,
            KeepAlive = false,
            PeerVerification = false,
            Timeout = 10,
            TimeoutEnabled = true,
            UserAgent = "NexusSystemAgent",
        };

        public HttpsRequestManager(string companyId, string groupId, string systemId, string secret, string apiUrl)
            : base(companyId, groupId, systemId, secret, apiUrl) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpsClient.Dispose();
            }
        }

        protected override void SendNextRequest()
        {
            // check that client isn't already busy
            if (httpsClient.ProcessBusy)
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
                var clientRequest = new HttpsClientRequest()
                {
                    RequestType = apiRequest.GetHttpsRequestType(),
                    Url = new Crestron.SimplSharp.Net.Http.UrlParser(apiRequest.Url),
                    Encoding = Encoding.UTF8,
                    ContentStream = apiRequest.Content,
                    KeepAlive = false,
                };

                // set request headers
                clientRequest.Header.ContentType = "application/json";
                clientRequest.Header.SetHeaderValue("Accept", "*/*");
                clientRequest.Header.SetHeaderValue("Secret", Secret);
                clientRequest.Header.SetHeaderValue("Expect", string.Empty); // remove Expect: 100-continue header which was causing 100 status code response

                // dispatch request asynchronously
                var result = httpsClient.DispatchAsync(clientRequest, ResponseCallback);
                if (result == HttpsClient.DISPATCHASYNC_ERROR.PENDING)
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

        private void ResponseCallback(HttpsClientResponse response, HTTPS_CALLBACK_ERROR error)
        {
            try
            {
                if (error == HTTPS_CALLBACK_ERROR.COMPLETED)
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