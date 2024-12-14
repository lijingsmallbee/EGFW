using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace NexgenDragon
{
    public class TimerManager:Singleton<TimerManager>,IManager,ITicker
    {
        class TimeContext
        {
            public float time;
            public Action callback;

            public Action<float> _tickCB;
            public Action _tickEnd;
            
            private float lifeTime;
            private bool needRemove;
            public void Update(float delta)
            {
                if (needRemove) return;
                
                lifeTime += delta;
                if(lifeTime > time)
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                    
                    needRemove = true;
                }
            }

            public bool NeedRemove=>needRemove;

            public TimeContext(float time,Action callback)
            {
                this.time = time;
                this.callback = callback;
            }

            public TimeContext(float time, Action<float> cb, Action endCB = null)
            {
                this.time = time;
                _tickCB = cb;
                _tickEnd = endCB;
            }

            public void TickUpdate(float delta)
            {
                _tickCB?.Invoke(delta);

                time -= delta;
                needRemove = time < 0f;
                if (needRemove)
                {
                    if (_tickEnd != null)
                    {
                        _tickEnd();
                        _tickEnd = null;
                    }
                }
            }
        }
        
        List<TimeContext> _allTimeContext = new List<TimeContext>();
        
        List<TimeContext> _tickerList = new List<TimeContext>();


        public void ExecuteAfterSecond(float time,Action action)
        {
            var context = new TimeContext(time, action);
            _allTimeContext.Add(context);
        }

        public void ExecuteTick(float time, Action<float> action, Action endCb = null)
        {
            var ctx = new TimeContext(time, action, endCb);
            _tickerList.Add(ctx);
        }

        public void Update(float delta)
        {
            if (_allTimeContext.Count > 0)
            {
                foreach(var context in _allTimeContext.ToArray())
                {
                    context.Update(delta);
                }
            
                _allTimeContext.RemoveAll((x) => x.NeedRemove);
            }

            if (_tickerList.Count > 0)
            {
                foreach (var ticker in _tickerList.ToArray())
                {
                    ticker.TickUpdate(delta);
                }

                _tickerList.RemoveAll(x => x.NeedRemove);
            }
        }
        

        public void Initialize(NexgenObject configParam)
        {
            GameFacade.Instance.AddTicker(this);
        }

        public void Reset()
        {
            _allTimeContext.Clear();
        }

        public void Tick(float delta)
        {
            Perf.BeginSample("VinayGao:TimerManager.Tick");
            Update(delta);
            Perf.EndSample();
        }
    }
}