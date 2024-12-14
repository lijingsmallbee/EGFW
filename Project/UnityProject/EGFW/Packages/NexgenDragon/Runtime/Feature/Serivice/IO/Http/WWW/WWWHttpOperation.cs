using UnityEngine;

namespace NexgenDragon
{
    public class WWWHttpOperation : NexgenObject, IHttpAsyncOperation
    {
        private AsyncOperation _operation;

        public bool IsDone
        {
            get
            {
                return _operation.isDone;
            }
        }

        public byte Priority
        {
            get
            {
                return (byte)_operation.priority;
            }
            set
            {
                _operation.priority = value;
            }
        }

        public float Progress
        {
            get
            {
                return _operation.progress;
            }
        }

        public void Abort()
        {
            
        }

        public WWWHttpOperation(AsyncOperation operation)
        {
            _operation = operation;
        }

        public override void Release()
        {
        }
    }
}
