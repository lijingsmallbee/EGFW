namespace NexgenDragon
{
    public interface IParticleVisualEffectCore
    {
        void Play(ParticleVisualEffect effect);
        void Stop(ParticleVisualEffect effect);
        void Simulate(ParticleVisualEffect effect, float dt);
        void SetLooping(ParticleVisualEffect effect, bool loop);
    }
}