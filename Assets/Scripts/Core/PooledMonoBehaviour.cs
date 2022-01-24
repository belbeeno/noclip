using UnityEngine;
using System.Collections;

public class PooledMonoBehaviour : MonoBehaviour
{
    public virtual void OnInstantiate() { }
    public virtual void OnSpawn() { } 
    public virtual void OnRecycle() { }
}
