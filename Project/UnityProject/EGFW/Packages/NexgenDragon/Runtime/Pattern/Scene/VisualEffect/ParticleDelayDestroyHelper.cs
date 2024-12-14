using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDelayDestroyHelper : MonoBehaviour
{
    bool startStopped = false;
    List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    System.Action callback;

    void Awake()
    {
        particleSystems.Clear();
        GetComponentsInChildren(true, particleSystems);
        startStopped = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (startStopped)
        {
            for (int i = 0; i < particleSystems.Count; i++)
            {
                if (!particleSystems[i].isStopped)
                {
                    return;
                }
            }
            callback?.Invoke();
            startStopped = false;
        }
    }

    public void StopWithCallback(System.Action callback)
    {
        startStopped = true;
        this.callback = callback;

        for (int i = 0; i < particleSystems.Count; i++)
        {
            particleSystems[i].Stop();
        }
    }
}
