using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleSequence : MonoBehaviour
{
    public RectTransform target;
    Vector2 anchor;

    [Header("Reveal")]
    public AudioSource wind;
    public AnimationCurve curveReveal;

    [Header("Waiting for Input")]
    public AudioSource intro;
    public Text text;

    [Header("Outro")]
    public AnimationCurve curveHide;

    float End(AnimationCurve curve) => curve.keys[curve.length - 1].time;

    private void OnEnable()
    {
        StartCoroutine(RevealCoroutine());
    }

    IEnumerator RevealCoroutine()
    {
        yield return new WaitForSeconds(1f);
        wind.Play();

        anchor = target.anchoredPosition;
        float timer = 0;
        float endTime = End(curveReveal);

        while (timer < endTime)
        {
            target.anchoredPosition = Vector2.up * curveReveal.Evaluate(timer) * anchor;
            timer += Time.deltaTime;
            yield return 0;
        }

        target.anchoredPosition = Vector2.up * curveReveal.Evaluate(endTime) * anchor;
        StartCoroutine(WaitForInput());
    }

    IEnumerator WaitForInput()
    {
        string msg = "- Press LMB to begin.";
        text.text = "-";
        yield return new WaitForSeconds(0.3f);
        WaitForSeconds pause = new WaitForSeconds(0.1f);
        bool lmb = false;
        for (int i= 1; i < msg.Length && !lmb; ++i)
        {
            lmb = Input.GetButton("Fire1");
            text.text = msg.Substring(0, i);
            yield return pause;
        }

        while (!lmb)
        {
            lmb = Input.GetButtonDown("Fire1");
            yield return 0;
        }
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        float timer = 0;
        float volumeDuration = 4f;

        intro.volume = 0f;
        intro.Play();
        while (intro.isPlaying)
        {
            if (!string.IsNullOrEmpty(text.text))
            {
                text.text = text.text.Substring(0, text.text.Length - 1);
            }
            intro.volume = Mathf.Clamp01(timer / volumeDuration);
            timer += Time.deltaTime;
            yield return 0;
        }

        timer = 0f;
        float end = End(curveHide);
        while (timer < end)
        {
            target.anchoredPosition = Vector2.up * curveHide.Evaluate(timer) * anchor;
            timer += Time.deltaTime;
            yield return 0;
        }
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }
}
