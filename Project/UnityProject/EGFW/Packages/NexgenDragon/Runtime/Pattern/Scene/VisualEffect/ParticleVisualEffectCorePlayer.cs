using UnityEngine;

namespace NexgenDragon
{
    public class ParticleVisualEffectCorePlayer : IParticleVisualEffectCore
    {        
        public void Play(ParticleVisualEffect effect)
        {         
            if (!effect)
            {
                return;
            }

            foreach (var system in effect.ParticleSystems)
            {
                system.Simulate(effect.simulateStartTime, false, true);
                system.Play(false);
            }
        }

        public void Stop(ParticleVisualEffect effect)
        {
            if (!effect)
            {
                return;
            }

            foreach (var system in effect.ParticleSystems)
            {
                system.Stop(false);
            }
        }

        public virtual void Simulate(ParticleVisualEffect effect, float dt)
        {
            if (!effect)
            {
                return;
            }

            foreach (var system in effect.ParticleSystems)
            {
                var main = system.main;
                main.simulationSpeed = effect.speed;
            }
        }

        public void SetLooping(ParticleVisualEffect effect, bool loop)
        {
            if (!effect)
            {
                return;
            }

            foreach (var system in effect.ParticleSystems)
            {
                var main = system.main;
                main.loop = loop;
            } 
        }
    }
}