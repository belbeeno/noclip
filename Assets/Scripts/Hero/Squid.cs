using System;
using UnityEngine;

public class Squid : PooledMonoBehaviour
{
    public ParticleSystem particles = null;

    public override void OnRecycle()
    {
        particles.Clear();
    }

    public override void OnSpawn()
    {
        particles.Play();
    }

    public void Update()
    {
        if (particles.isStopped)
        {
            this.Recycle();
        }
    }
}