using System;
using UnityEngine;
using UnityEngine.Networking;

namespace NexgenDragon
{
    public class AssetHttpOperation : IHttpAsyncOperation
    {
        private readonly UnityWebRequestAsyncOperation _operation;

        public AssetHttpOperation(UnityWebRequestAsyncOperation operation)
        {
            _operation = operation;
        }

        public UnityWebRequest Request
        {
            get { return _operation.webRequest; }
        }

        public bool IsDone
        {
            get { return _operation.isDone; }
        }

        public byte Priority
        {
            get { return (byte) _operation.priority; }
            set { _operation.priority = value; }
        }

        public float Progress
        {
            get { return _operation.progress; }
        }

        public bool IsAborted { get; private set; }

        public void Abort()
        {
            try
            {
                _operation.webRequest.Abort();
                IsAborted = true;
            }
            catch
            {
                // 如果请求应经调用过Dispose()，会抛ArgumentNullException
            }
        }
    }
}