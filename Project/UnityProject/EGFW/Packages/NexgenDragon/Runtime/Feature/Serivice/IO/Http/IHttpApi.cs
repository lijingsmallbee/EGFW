using System;

namespace NexgenDragon
{
	public interface IHttpAsyncOperation : IAsyncOperation
	{
		void Abort();
	}

	public interface IHttpApi : IObject
	{
		IHttpAsyncOperation Send(HttpRequestData request, Action<HttpResponseData> response);
		bool IsThreadSupported { get; }
		bool EnableProxy { get; set; }
		string ProxyIP { get; set; }
		int ProxyPort { get; set; }
		void Reset();
        void Tick(float dt);
	}
}

