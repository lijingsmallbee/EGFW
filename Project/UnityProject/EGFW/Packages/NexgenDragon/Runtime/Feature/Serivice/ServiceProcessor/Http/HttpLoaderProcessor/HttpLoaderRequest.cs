using System;

namespace NexgenDragon
{
	public class HttpLoaderRequest
	{
		public HttpLoaderRequest(BaseServiceParameter parameter, Action<bool, BaseServiceResponse, HttpRequestor.BaseProtocol> callback, object userData)
		{
			Parameter = parameter;
			Callback = callback;
			UserData = userData;
		}

		public BaseServiceParameter Parameter {get; set;}
		public Action<bool, BaseServiceResponse, HttpRequestor.BaseProtocol> Callback {get; set;}
		public object UserData;

		public HttpRequestor HttpRequestor;

		private HttpRequestData _httpRequestData = null;

        public HttpRequestData GetHttpRequestData()
        {
            if (_httpRequestData == null)
            {
                _httpRequestData = new HttpRequestData
                {
                    url = Parameter.GetUrl(),
					requestContent = Parameter.Encode(0)
                };

				Parameter.GetHttpHeader(_httpRequestData.headersDict);
            }
            return _httpRequestData;
        }
	}
}

