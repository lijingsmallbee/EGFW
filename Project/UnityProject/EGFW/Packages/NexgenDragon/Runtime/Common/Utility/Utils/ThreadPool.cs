using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WaitCallback = System.Threading.WaitCallback;

namespace Unity.Entities
{
    public static class ThreadPool
    {
        public static bool QueueUserWorkItem(WaitCallback callBack, object state = null)
        {
            Action cb = () => callBack?.Invoke(state);
            var jobId = GenJobId();
            var r = _jobIdToInfo.TryAdd(jobId, new JobInfo(cb));
            Debug.Assert(r);

            new Job
            {
                id = jobId,
                funcPtr = _jobFuncPtr
            }.Schedule();
            return true;
        }

        static readonly ConcurrentDictionary<int, JobInfo> _jobIdToInfo = new ConcurrentDictionary<int, JobInfo>();

        static int _jobId;
        static int GenJobId()
        {
            if (_jobId == int.MaxValue)
                _jobId = 1;
            else
                ++_jobId;
            return _jobId;
        }

        static FunctionPointer<JobFuncDelegate> __jobFuncPtr;
        static ref FunctionPointer<JobFuncDelegate> _jobFuncPtr
        {
            get
            {
                if (!__jobFuncPtr.IsCreated)
                    __jobFuncPtr = new FunctionPointer<JobFuncDelegate>(Marshal.GetFunctionPointerForDelegate<JobFuncDelegate>(JobFunc));
                return ref __jobFuncPtr;
            }
        }

        delegate void JobFuncDelegate(int jobId);
        [AOT.MonoPInvokeCallback(typeof(JobFuncDelegate))]
        static void JobFunc(int jobId)
        {
            var r = _jobIdToInfo.TryRemove(jobId, out var jobInfo);
            Debug.Assert(r);

            try { jobInfo.Execute(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        struct Job : IJob
        {
            public int id;
            public FunctionPointer<JobFuncDelegate> funcPtr;

            public void Execute() => funcPtr.Invoke(id);
        }

        readonly struct JobInfo
        {
            public JobInfo(Action callBack) => _callBack = callBack;

            public void Execute() => _callBack();

            readonly Action _callBack;
        }
    }
}
