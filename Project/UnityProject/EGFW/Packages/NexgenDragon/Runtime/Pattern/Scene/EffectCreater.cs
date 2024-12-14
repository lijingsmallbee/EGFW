using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NexgenDragon
{
    public class EffectCreater
    {
        public enum CreateState
        {
            none,
            creating,
            done,
        }

        private VisualEffectHandle _req;

        public VisualEffect Effect
        {
            get
            {
                if (_req != null)
                {
                    return _req.Effect;
                }

                return null;
            }
        }

        public void Create(string effectName, string pool, Transform parent, bool syncCreate = false)
        {
            _req = VisualEffectManager.Instance.Create(effectName, pool, parent, null, syncCreate);
        }

        public void Create(string effectName, string pool, Transform parent, Vector3 localPos, bool syncCreate = false)
        {
            _req = VisualEffectManager.Instance.Create(effectName, pool, parent, (success, effect) =>
            {
                _req = null;
                if (success)
                {
                    if (parent != null)
                    {
                        effect.Effect.gameObject.transform.localPosition = localPos;
                    }
                }
            }, syncCreate);
        }

        public void Create(string effectName, string pool, Transform parent, System.Action callback,
            bool syncCreate = false)
        {
            _req = VisualEffectManager.Instance.Create(effectName, pool, parent, (success, effect) =>
            {
                if (callback != null)
                {
                    callback();
                }
            }, syncCreate);
        }


        public void Destroy()
        {

            if (_req != null)
            {
                _req.Delete();
            }
        }

        public CreateState GetCreateState()
        {
            if (_req != null)
            {
                if (!_req.Effect)
                {
                    return CreateState.creating;
                }
                else
                {
                    return CreateState.done;
                }
            }
            return CreateState.none;
        }
    }
}
