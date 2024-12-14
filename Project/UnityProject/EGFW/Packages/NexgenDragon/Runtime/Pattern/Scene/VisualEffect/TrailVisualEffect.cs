using UnityEngine;

namespace NexgenDragon
{
    public class TrailVisualEffect : VisualEffect
    {
        private TrailRenderer[] _trails;

        private void InitOnce()
        {
            if (_trails == null)
            {
                _trails = GetComponentsInChildren<TrailRenderer> (true);
            }
        }

        public override void Play()
        {
            InitOnce();
            
            foreach (var trail in _trails)
            {
                trail.enabled = true;
            }
        }

        public override void Stop()
        {
            InitOnce();
            
            foreach (var trail in _trails)
            {
                trail.Clear();
                trail.enabled = false;
            }
        }

        public override void Simulate(float dt)
        {
            InitOnce();
        }

        public override void SetLooping(bool loop)
        {
            
        }

        public override bool IsPlaying()
        {
            throw new System.NotImplementedException();
        }
    }
}