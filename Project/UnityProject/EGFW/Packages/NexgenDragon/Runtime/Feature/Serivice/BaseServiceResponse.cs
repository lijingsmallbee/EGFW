using System;
using System.Collections.Generic;

namespace NexgenDragon
{
    public abstract class BaseServiceResponse : NexgenObject
	{
		public BaseServiceParameter param; // push没有参数
        // public object payload;
        public object payload_lua;
        public long errorCode;
		
		private object _userData; // 透传
		private Dictionary<string, string> _headers;

        public BaseServiceResponse(BaseServiceParameter param, object payload_lua)
        {
			this.param = param;
            // this.payload = payload;
			this.payload_lua = payload_lua;
        }

		public void SetHeaders(Dictionary<string, string> headers)
		{
			_headers = headers;
		}

		public Dictionary<string, string> GetHeaders()
		{
			return _headers;
		}

		public void SetUserData(object userData)
		{
			this._userData = userData;
		}

		public object GetUserData()
		{
			return _userData;
		}

		public override void Release ()
		{
			base.Release ();

			param = null;
			// payload = null;
			payload_lua = null;
			errorCode = 0;
			_userData = null;
		}

        public abstract object Decode(Type type);
	}
}
