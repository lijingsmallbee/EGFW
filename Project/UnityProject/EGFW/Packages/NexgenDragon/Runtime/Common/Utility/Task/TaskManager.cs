#if false // 使用TaskPoolManager代替

using System.Collections.Generic;
using System;
using System.Threading;
using NexgenDragon;
using UnityEngine.Profiling;

public class TaskManager : Singleton<TaskManager>, ITicker, IManager
{
    private const int MaxThreads = 1;

    // 主线程子线程都需要访问的变量（需要加锁）
    private int _numThreads;
    private readonly Queue<Action> _childThreadedActions = new Queue<Action>();
    private readonly List<Action> _mainThreadedActions = new List<Action>();
    
    // 只有主线程要访问的变量（不需要加锁）
    private readonly List<Action> _currentActions = new List<Action>();

    public void Initialize(NexgenObject configParam)
    {
        GameFacade.Instance.AddTicker(this);
    }

    public void Reset()
    {
        
    }

    public override void Release()
    {
        GameFacade.Instance.RemoveTicker(this);
    }

    public void QueueOnMainThread(Action action)
    {
        lock (_mainThreadedActions)
        {
            _mainThreadedActions.Add(action);
        }
    }

    public void RunAsync(Action a)
    {
        lock (_childThreadedActions)
        {
            _childThreadedActions.Enqueue(a);
        }
    }

    private void RunAction(object action)
    {
        try
        {
            ((Action)action)();
        }
        finally
        {
            Interlocked.Decrement(ref _numThreads);
        }
    }

    // Update is called once per frame
    public void Tick(float delta)
    {
        Perf.BeginSample("VinayGao:TaskManager.Tick");
        // do sub thread actions
        lock (_childThreadedActions)
        {
            if (_numThreads < MaxThreads && _childThreadedActions.Count > 0)
            {
                Interlocked.Increment(ref _numThreads);
                Unity.Entities.ThreadPool.QueueUserWorkItem(RunAction, _childThreadedActions.Dequeue());
            }
        }

        // do main queue actions
        lock (_mainThreadedActions)
        {
            _currentActions.Clear();
            _currentActions.AddRange(_mainThreadedActions);
            _mainThreadedActions.Clear();
        }
        
        foreach(var a in _currentActions)
        {
            a();
        }
        Perf.EndSample();
    }
}

#endif