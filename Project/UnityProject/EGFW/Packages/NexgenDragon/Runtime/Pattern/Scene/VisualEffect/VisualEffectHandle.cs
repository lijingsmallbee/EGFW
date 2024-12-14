using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NexgenDragon
{
    public class VisualEffectHandle
    {
        private GameObjectRequest _request;
        private readonly GameObjectPool _pool;
        
        //private ParticleDelayDestroyHelper particleDelayDestroyHelper;
        private bool _enable = true;
        
        // Simulate
        private float _delayTime;
        private float _playTime;

        private Action<bool, VisualEffectHandle> _playFinishCallback;
        
        internal VisualEffectHandle(GameObjectPool pool)
        {
            _pool = pool;
        }
        
        internal VisualEffectHandle(GameObject go, Transform parent,bool keepWorld = false)
        {
            if (!go)
            {
                return;
            }
            
            Asset = go;
            Asset.transform.SetParent(parent);
            HandleEffect(parent, go,keepWorld);
        }

        internal void Create(string vfxName, Transform parent, Action<bool, VisualEffectHandle> callback, bool syncCreate = false, int priority = 0, Action<bool, VisualEffectHandle> playFinishCallback = null)
        {
            _request = _pool.Create(vfxName, parent, delegate(GameObject go, object data)
            {
                _request = null; // 标记加载完毕
                if (go)
                {
                    Asset = go;
                    Asset.transform.SetParent(parent);
                    //particleDelayDestroyHelper = go.GetComponent<ParticleDelayDestroyHelper>();
                    //if (particleDelayDestroyHelper == null)
                    //{
                    //    particleDelayDestroyHelper = go.AddComponent<ParticleDelayDestroyHelper>();
                    //}
                    UpdateEnable();
                    
                    HandleEffect(parent, go);

                    callback?.Invoke(true, this);

                    _playFinishCallback = playFinishCallback;
                }
                else
                {
                    callback?.Invoke(false, this);
                }
            }, null, priority, syncCreate);
        }

        private void HandleEffect(Transform parent, GameObject go,bool keepWorld = false)
        {
            Effect = go.GetComponentInChildren<VisualEffect>(true);
            if (Effect)
            {
                if(!keepWorld)
                {
                    Effect.Attach(parent);
                }               

                Stop();

                if (Effect.updateLoopingOnAwake)
                {
                    Effect.SetLooping(Effect.loopingOnAwake);
                }

                if (Effect.autoPlay)
                {
                    Play();
                }
            }
        }

        public void Delete()
        {
            if (!IsAlive)
            {
                return;
            }

            if (_request != null)
            {
                _request.Cancel();
                _request = null;
            }

            if (_pool != null)
            {
                _pool.Destroy (Asset);
            }
            else
            {
                if(Application.isPlaying)
                {
                    Object.Destroy(Asset);
                }
                else
                {
                    // 编辑器预览使用这段逻辑
                    Object.DestroyImmediate(Asset);
                }
            }

            Asset = null;
            _playFinishCallback = null;
        }

        public bool IsAlive => _request != null || Asset != null;

        public GameObject Asset { get; private set; }

        public VisualEffect Effect { get; private set; }

        public bool IsPlaying { get; private set; }

        public string PoolName => _pool.Name;

        public void Play()
        {
            IsPlaying = true;
            _delayTime = 0;
            _playTime = 0;
            if (Effect)
            {
                Effect.Play();
            }
        }

        public void Stop()
        {
            IsPlaying = false;
            if (Effect)
            {
                Effect.Stop();
            }
        }

        public void DelayDelete(float delay)
        {

            //if (particleDelayDestroyHelper != null)
            //{
            //    particleDelayDestroyHelper.StopWithCallback(()=>{
            //        Delete();
            //    });
            //}
            //else
            TimerManager.Instance.ExecuteAfterSecond(delay, () =>
            {
                Delete();
            });            
        }

        public void OnUpdate(float dt)
        {
            if (Effect == null || !IsPlaying)
            {
                return;
            }

            dt = Effect.speed * dt;
            
            _delayTime += dt;
            if (Effect.delay > 0 && _delayTime < Effect.delay)
            {
                return;
            }

            _playTime += dt;
            Effect.Simulate(dt);

            if (Effect.duration >= 0 && _playTime > Effect.duration)
            {
                Stop();
                
                if (null != _playFinishCallback)
                {
                    _playFinishCallback.Invoke(true, this);
                }

                if (Effect.autoDisable)
                {
                    SetEnable(false);
                }

                if (Effect.autoDelete)
                {
                    Delete();
                }
            }
        }

        public void SetEnable(bool enable)
        {
            _enable = enable;
            UpdateEnable();
        }

        private void UpdateEnable()
        {
            if (Asset)
            {
                Asset.SetActive(_enable);
            }
        }
    }
}
