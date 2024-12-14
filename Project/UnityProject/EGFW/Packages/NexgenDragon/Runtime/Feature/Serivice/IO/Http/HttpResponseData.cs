using System.Collections.Generic;

namespace NexgenDragon
{
    public class HttpResponseData : NexgenObject
	{
		public long responseCode;
		public byte[] responseContent;
        public string url;
        public string savePath;
        public ulong downloadedBytes;
		public Dictionary<string, List<string>> headers;
	}
}

