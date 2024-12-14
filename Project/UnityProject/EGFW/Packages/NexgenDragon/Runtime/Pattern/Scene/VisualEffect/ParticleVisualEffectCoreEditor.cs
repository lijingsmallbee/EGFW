using UnityEngine;

namespace NexgenDragon
{
    public class ParticleVisualEffectCoreEditor : ParticleVisualEffectCorePlayer
    {
        public override void Simulate(ParticleVisualEffect effect, float dt)
        {
            if (!effect)
            {
                return;
            }
          
            foreach (var system in effect.ParticleSystems)
            {                       
                system.Simulate(dt, false, false);
            }
        }
    }
}
