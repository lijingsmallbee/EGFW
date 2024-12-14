using System;
using UnityEngine;
using NexgenDragon;
namespace NexgenDragon
{
    public class GizmosHelper : MonoSingleton<GizmosHelper>
    {
        public event Action OnDrawGizmosEvent;
        private void OnDrawGizmos()
        {
            if (OnDrawGizmosEvent != null)
            {
                OnDrawGizmosEvent();
            }
        }

        public void AddCallback(Action draw)
        {
            OnDrawGizmosEvent += draw;
        }

        public void RemoveCallback(Action draw)
        {
            OnDrawGizmosEvent -= draw;
        }
    }
}
