using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeDriver : MonoBehaviour
{
    public float duration = 1f;
    public Image image = null;

    private void OnEnable()
    {
        StartCoroutine(FadeCoroutine(1, -1, 1f, duration));
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void FadeToBlack(float waitForSeconds)
    {
        StartCoroutine(FadeCoroutine(-1, 1, waitForSeconds, duration));
    }

    public void CutToBlack()
    {
        StartCoroutine(FadeCoroutine(-1, 1, 0f, 0.1f));
    }

    private IEnumerator FadeCoroutine(float from, float to, float wait, float duration)
    {
        image.material.SetFloat("_Value", from);
        yield return new WaitForSeconds(wait);

        float timer = 0f;
        float t = from;

        while (timer < duration)
        {
            image.material.SetFloat("_Value", t);
            //Debug.Log("setting alpha to " + image.material.GetFloat("_Value"));
            yield return 0;
            timer += Time.unscaledDeltaTime;
            t = Mathf.Lerp(from, to, timer / duration);
        }
        image.material.SetFloat("_Value", to);
    }

}
