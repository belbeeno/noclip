using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class YouFuckedUp : MonoBehaviour
{
    public AudioSource needCard;

    public void TriggerGoodEnd()
    {
        StartCoroutine(Thread("GoodEnd", 0f));
    }
    public void TriggerBadEnd()
    {
        StartCoroutine(Thread("BadEnd", 5f));
    }

    IEnumerator Thread(string goTo, float waitForSecs)
    {
        yield return new WaitForSeconds(waitForSecs);
        SceneManager.LoadScene(goTo, LoadSceneMode.Single);
    }
}
