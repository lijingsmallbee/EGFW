using NexgenDragon;
using UnityEngine;

public class AnimationVisualEffect : VisualEffect
{
    private Animation _animation;
    
    private void InitOnce()
    {
        if (_animation == null)
        {
            _animation = GetComponentInChildren<Animation>(true);
        }
    }
    
    public override void Play()
    {
        InitOnce();

        if (_animation)
        {
            _animation.Rewind();
            _animation.Play(PlayMode.StopAll);
        }
    }

    public override void Stop()
    {
        InitOnce();

        if (_animation)
        {
            _animation.Stop();
        }
    }

    public override void Simulate(float dt)
    {
        
    }

    public override void SetLooping(bool loop)
    {
        
    }
    
    public override bool IsPlaying()
    {
        throw new System.NotImplementedException();
    }
}