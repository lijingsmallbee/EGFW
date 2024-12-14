using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
    public class ParticleVisualEffect : VisualEffect
    {
        private IParticleVisualEffectCore _playerEffectCore;
        
        public List<ParticleSystem> ParticleSystems { get; } = new List<ParticleSystem>();

        private void InitOnce()
        {
            if (_playerEffectCore == null)
            {
                _playerEffectCore = Application.isPlaying ? new ParticleVisualEffectCorePlayer() : new ParticleVisualEffectCoreEditor();
            }

            if (ParticleSystems.Count == 0)
            {
                GetComponentsInChildren(true, ParticleSystems);
                if (duration <= 0)
                {
                    foreach (var system in ParticleSystems)
                    {
                        duration = Mathf.Max(duration, system.main.duration);
                    } 
                }
            }
        }
        
        //2024.10.22 max+
        void OnEnable()
        {
            var trailRenderers = GetComponentsInChildren<TrailRenderer>();
            foreach (var trailRenderer in trailRenderers)
            {
                trailRenderer.Clear();
            }
        }
       

        public override void Play()
        {
            InitOnce();
            
            _playerEffectCore.Play(this);
        }

        public override void Stop()
        {
            InitOnce();

            _playerEffectCore.Stop(this);
        }

        public override void Simulate(float dt)
        {
            InitOnce();

            _playerEffectCore.Simulate(this, dt);
        }

        public override void SetLooping(bool loop)
        {
            InitOnce();

            _playerEffectCore.SetLooping(this, loop);
        }
        
        public override bool IsPlaying()
        {
            foreach (var system in ParticleSystems)
            {
                if (system.isPlaying)
                {
                    return true;
                }
            }

            return false;
        }
    }
}