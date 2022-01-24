using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footstep : MonoBehaviour
{
    public AudioSource[] sources = new AudioSource[2];
    public AudioClip[] steps = new AudioClip[0];
    
    public void Step(int idx)
    {
        if (idx >= sources.Length || sources[idx].isPlaying || steps.Length <= 0) return;

        sources[idx].clip = steps[Random.Range(0, steps.Length - 1)];
        sources[idx].Play();
    }
}
