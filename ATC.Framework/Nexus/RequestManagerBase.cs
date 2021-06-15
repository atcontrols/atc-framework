using ATC.Framework.Debugging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace ATC.Framework.Nexus
{
    internal interface IRequestManager : ISystemComponent, IDisposable
    {
        string ApiUrl { get; set; }
        string CompanyId { get; set; }
        string GroupId { get; set; }
        string SystemId { get; set; }
        string Secret { get; set; }

        /// <summary>
        /// Send an HTTP request to the server.
        /// </summary>
        /// <param name="method">The HTTP method to use (e.g. GET)</param>
        /// <param name="url">The address of the resource.</param>
        /// <param name="body">The body of the message.</param>
        void SendRequest(HttpMethod method, string url, string body);

        /// <summary>
        /// Send an HTTP request to the server.
        /// </summary>
        /// <param name="method">The HTTP method to use (e.g. GET)</param>
        /// <param name="url">The address of the resource.</param>
        /// <param name="obj">The body object (will be serialized).</param>
        void SendRequest(HttpMethod method, string url, object obj);
    }

    internal abstract class RequestManagerBase : SystemComponent, IRequestManager
    {
        #region Fields

        protected readonly Queue<ApiRequest> requestQueue = new Queue<ApiRequest>();

        #endregion

        #region Properties

        /// <summary>
        /// The address of the API server (default: https://api.nexus.atcontrols.com.au)
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// The Nexus Company ID that the system belongs to.
        /// </summary>
        public string CompanyId { get; set; }

        /// <summary>
        /// The Nexus Group ID that the system belongs to.
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// The Nexus System ID that is associated with the system.
        /// </summary>
        public string SystemId { get; set; }

        /// <summary>
        /// The system secret (password) to enable writing to the system.
        /// </summary>
        public string Secret { get; set; }

        #endregion

        #region Constructor

        public RequestManagerBase(string companyId, string groupId, string systemId, string secret, string apiUrl)
        {
            ApiUrl = apiUrl;
            CompanyId = companyId;
            GroupId = groupId;
            SystemId = systemId;
            Secret = secret;
        }

        #endregion

        #region Public methods

        public void SendRequest(HttpMethod method, string url, string body)
        {
            requestQueue.Enqueue(new ApiRequest(method, url, body));

            if (requestQueue.Count == 1)
                SendNextRequest();
            else
                Trace("SendRequest() enqueued new request. Count is: " + requestQueue.Count);
        }

        public void SendRequest(HttpMethod method, string url, object obj)
        {
            string body = SerializeObject(obj);
            SendRequest(method, url, body);
        }
        
        #endregion

        #region Protected methods

        protected abstract void SendNextRequest();

        protected void AdvanceQueue()
        {
            // remove item from queue and dispose of it
            var removedItem = requestQueue.Dequeue();
            removedItem.Dispose();

            // if there's anything more in the queue, send it
            if (requestQueue.Count > 0)
                SendNextRequest();
        }

        #endregion

        #region Private methods
        private string SerializeObject(object obj)
        {
            JsonConverter[] jsonConverters = new JsonConverter[] { new StringEnumConverter(), new IsoDateTimeConverter() };
            string json = JsonConvert.SerializeObject(obj, jsonConverters);
            Trace("SerializeObject() output below\r\n:" + json, TraceLevel.Extended);

            return json;
        }
        #endregion
    }
}
