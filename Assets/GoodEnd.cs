using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoodEnd : MonoBehaviour
{
    public bool triggerOnce = false;
    private bool triggered = false;

    private void OnTriggerEnter(Collider collision)
    {
        HeroMovement hm = collision.GetComponent<HeroMovement>();
        Debug.Assert(hm != null);
        if (hm.gotIt)
        {
            SceneManager.LoadScene("GoodEnd", LoadSceneMode.Single);
            return;
        }

        if (triggerOnce && triggered) return;
        var audio = GetComponent<AudioSource>();
        if (audio != null && !audio.isPlaying) audio.Play();
        triggered = true;
    }
}
