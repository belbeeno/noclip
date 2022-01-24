using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public AudioSource gameOverAudio;
    bool canClick = false;
    public string msg = "- Game over";
    public UnityEngine.UI.Text text;
    public float textPause = 1f;

    private void OnEnable()
    {
        StartCoroutine(Thread());
    }

    IEnumerator Thread()
    {
        yield return new WaitForSeconds(textPause);
        WaitForSeconds pause = new WaitForSeconds(0.3f);
        for (int i = 0; i < msg.Length; ++i)
        {
            text.text = msg.Substring(0, i);
            yield return pause;
        }
        text.text = msg;
        canClick = true;
    }

    // Update is called once per frame
    void Update()
    {
        if ((canClick && Input.GetButtonUp("Fire1")) || !gameOverAudio.isPlaying)
        {
            Application.Quit();
        }
    }
}
