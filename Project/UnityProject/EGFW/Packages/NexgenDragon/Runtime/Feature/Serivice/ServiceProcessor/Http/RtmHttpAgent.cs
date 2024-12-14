using System;
using System.Collections.Generic;

namespace NexgenDragon
{
    public class RtmHttpAgent : IHttpApi
    {
        private IRtmApi _rtm;

        public RtmHttpAgent(IRtmApi rtm)
        {
            _rtm = rtm;
        }
        
        public IHttpAsyncOperation Send(HttpRequestData request, Action<HttpResponseData> callback)
        {
            var operation = new RtmHttpAsyncOperation
            {
                IsDone = false,
                Priority = 0,
                Progress = 0
            };
            
            var sentData = new RtmData
            {
                Header = request.headersDict,
                Body = request.requestContent
            };

            _rtm.Send(sentData, delegate(RtmData receivedData, int statusCode)
            {
                var responseData = new HttpResponseData
                {
                    responseCode = statusCode,
                    responseContent = receivedData.Body,
                    downloadedBytes = receivedData.Body != null ? (ulong) receivedData.Body.Length : 0,
                    url = request.url,
                };

                var header = receivedData.Header;
                if (header != null)
                {
                    responseData.headers = new Dictionary<string, List<string>>();
                    foreach (var pair in header)
                    {
                        responseData.headers[pair.Key] = new List<string> {pair.Value};
                    }
                }

                operation.IsDone = true;
                operation.Progress = 1;

                if (callback != null)
                {
                    callback(responseData);
                }
            });

            return operation;
        }

        public bool IsThreadSupported
        {
            get { return false; }
        }
        
        public bool EnableProxy { get; set; }
        public string ProxyIP { get; set; }
        public int ProxyPort { get; set; }
        
        public void Release()
        {
            
        }
        
        public void Reset()
        {
            
        }

        public void Tick(float dt)
        {

        }


    }
}