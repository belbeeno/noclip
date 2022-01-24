using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Collect : MonoBehaviour
{
    public AudioSource toPlay;
    public UnityEvent OnPickup;

    private void OnTriggerEnter(Collider other)
    {
        other.gameObject.SendMessage("SecretCollect");
        OnPickup.Invoke();
    }
}
