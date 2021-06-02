using Crestron.SimplSharp.Net.Http;
using System;

namespace ATC.Framework.Communications
{
    public class HttpRequester : SystemComponent
    {
        public event EventHandler<RequestResponseEventArgs> RequestResponseHandler;

        #region Public methods
        public bool Get(string url)
        {
            if (url.StartsWith(@"http://"))
                return GetHttp(url);
            else
            {
                TraceError("Get() unhandled url.");
                return false;
            }
        }
        #endregion

        #region Private methods
        private bool GetHttp(string url)
        {
            var client = new HttpClient();
            var result = client.GetAsync(url, GetHttpCallback);
            if (result == HttpClient.DISPATCHASYNC_ERROR.PENDING)
            {
                Trace(String.Format("GetHttp() successfully requested url: {0}", url));
                return true;
            }
            else
            {
                TraceError("GetHttp() error in dispatching HTTP request: " + result);
                return false;
            }
        }
        #endregion

        #region Callback methods
        private void GetHttpCallback(string response, HTTP_CALLBACK_ERROR error)
        {
            if (error == HTTP_CALLBACK_ERROR.COMPLETED)
            {
                TraceInfo("GetHttpCallback() successfully completed http request.");

                // invoke event handler
                if (RequestResponseHandler != null)
                    RequestResponseHandler(this, new RequestResponseEventArgs(response));
            }
            else
                TraceError("GetHttpCallback() error occurred: " + error);
        }
        #endregion
    }

    public class RequestResponseEventArgs : EventArgs
    {
        public string Response { get; set; }

        public RequestResponseEventArgs(string response)
        {
            Response = response;
        }
    }
}
