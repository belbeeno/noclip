using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReloader : MonoBehaviour
{
    public float duration = 1f;
    public float cutDuration = 0.2f;
    public void ReloadScene()
    {
        StartCoroutine(ReloadSceneThread(duration));
    }
    public void CutReloadScene()
    {
        StartCoroutine(ReloadSceneThread(cutDuration));
    }

    IEnumerator ReloadSceneThread(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }
}
