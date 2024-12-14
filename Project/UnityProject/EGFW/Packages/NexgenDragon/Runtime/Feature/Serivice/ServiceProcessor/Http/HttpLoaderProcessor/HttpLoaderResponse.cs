using System;

namespace NexgenDragon
{
	public class HttpLoaderResponse
	{
		public HttpLoaderResponse(HttpLoaderRequest httpLoaderServiceRequest, HttpRequestor.BaseProtocol protocol, long responseCode)
		{
			HttpLoaderServiceRequest = httpLoaderServiceRequest;
			Protocol = protocol;
			ResponseCode = responseCode;
		}

		public HttpLoaderRequest HttpLoaderServiceRequest;
		public HttpRequestor.BaseProtocol Protocol;
		public long ResponseCode;
	}
}

