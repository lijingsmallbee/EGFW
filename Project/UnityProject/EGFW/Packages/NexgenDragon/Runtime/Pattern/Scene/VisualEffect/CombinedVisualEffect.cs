using System.Collections.Generic;

namespace NexgenDragon
{
    public class CombinedVisualEffect : VisualEffect
    {
        private readonly List<VisualEffect> _effects = new List<VisualEffect>();

        private void InitOnce()
        {
            if (_effects.Count == 0)
            {
                GetComponentsInChildren(true, _effects);
            }
        }

        public override void Play()
        {
            InitOnce();
            
            foreach (var effect in _effects)
            {
                if (!IsCombinedVisualEffect(effect))
                {
                    effect.Play();
                }
            }
        }

        public override void Stop()
        {
            InitOnce();
            
            foreach (var effect in _effects)
            {
                if (!IsCombinedVisualEffect(effect))
                {
                    effect.Stop();
                }
            }
        }

        public override void Simulate(float dt)
        {
            InitOnce();
            
            foreach (var effect in _effects)
            {
                if (!IsCombinedVisualEffect(effect))
                {
                    effect.Simulate(dt);
                }
            }
        }

        public override void SetLooping(bool loop)
        {
            foreach (var effect in _effects)
            {
                if (!IsCombinedVisualEffect(effect))
                {
                    effect.SetLooping(loop);
                }
            }
        }

        private static bool IsCombinedVisualEffect(VisualEffect effect)
        {
            return effect is CombinedVisualEffect;
        }
        
        public override bool IsPlaying()
        {
            throw new System.NotImplementedException();
        }
    }
}
