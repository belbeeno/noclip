using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oof : MonoBehaviour
{
    public bool triggerOnce = false;
    private bool triggered = false;

    private void OnTriggerEnter(Collider collision)
    {
        if (triggerOnce && triggered) return;
        var audio = GetComponent<AudioSource>();
        if (audio != null && !audio.isPlaying) audio.Play();
        triggered = true;
    }
}
