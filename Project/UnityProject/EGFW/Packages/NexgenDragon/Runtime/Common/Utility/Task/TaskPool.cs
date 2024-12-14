using System.Collections.Generic;
using System;
using System.Threading;
using NexgenDragon;

namespace NexgenDragon
{
    public class TaskPool : ITicker
    {
        private const int MaxThreads = 1;

        // 主线程子线程都需要访问的变量（需要加锁）
        private int _numThreads;
        private readonly Queue<Action> _childThreadedActions = new Queue<Action>();
        private readonly List<Action> _mainThreadedActions = new List<Action>();

        // 只有主线程要访问的变量（不需要加锁）
        private readonly List<Action> _currentActions = new List<Action>();
        
        private readonly string _name;

        public TaskPool(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        public void Reset()
        {
            lock (_childThreadedActions)
            {
                _childThreadedActions.Clear();
            }
            lock (_mainThreadedActions)
            {
                _mainThreadedActions.Clear();
            }
            _currentActions.Clear();
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
                ((Action) action)();
            }
            finally
            {
                Interlocked.Decrement(ref _numThreads);
            }
        }

        // Update is called once per frame
        public void Tick(float delta)
        {
            // do sub thread actions
            lock (_childThreadedActions)
            {
                if (_numThreads < MaxThreads && _childThreadedActions.Count > 0)
                {
                    Interlocked.Increment(ref _numThreads);
                    ThreadPool.QueueUserWorkItem(RunAction, _childThreadedActions.Dequeue());
                }
            }

            // do main queue actions
            lock (_mainThreadedActions)
            {
                if (_currentActions.Count > 0)
                    _currentActions.Clear();
                if (_mainThreadedActions.Count > 0)
                {
                    _currentActions.AddRange(_mainThreadedActions);
                    _mainThreadedActions.Clear();
                }
            }

            //
            if (_currentActions.Count > 0)
            {
                for (int i = 0; i < _currentActions.Count; i++)
                {
                    _currentActions[i]?.Invoke();
                }
            }
        }
    }
}