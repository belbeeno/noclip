using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class ObjectPool : MonoBehaviour
{
    static ObjectPool _instance;

    Dictionary<Component, List<Component>> objectLookup = new Dictionary<Component, List<Component>>();
    Dictionary<Component, Component> prefabLookup = new Dictionary<Component, Component>();

    void OnDestroy()
    {
        objectLookup.Clear();
        prefabLookup.Clear();
    }

    public static void CreatePool<T>(T prefab) where T : Component
    {
        if (instance == null) return;
        if (!instance.objectLookup.ContainsKey(prefab))
            instance.objectLookup.Add(prefab, new List<Component>());
    }

    public static void RemovePool<T>(T prefab) where T : Component
    {
        if (instance == null) return;
        instance.objectLookup.Remove(prefab);
    }

    public static bool HasPool<T>(T prefab) where T : PooledMonoBehaviour
    {
        if (instance == null) return false;
        return (instance.objectLookup.ContainsKey(prefab));
    }

    public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : PooledMonoBehaviour
    {
        if (instance == null) return null;

        if (instance.objectLookup.ContainsKey(prefab))
        {
            T obj = null;
            var list = instance.objectLookup[prefab];
            if (list.Count > 0)
            {
                while (obj == null && list.Count > 0)
                {
                    obj = list[0] as T;
                    list.RemoveAt(0);
                }
                if (obj != null)
                {
                    obj.transform.SetParent(null, false);
                    obj.transform.localPosition = position;
                    obj.transform.localRotation = rotation;
                    obj.gameObject.SetActive(true);
                    obj.OnSpawn();
                    instance.prefabLookup.Add(obj, prefab);
                    return obj;
                }
            }
            obj = (T)Object.Instantiate(prefab, position, rotation);
            obj.OnInstantiate();
            obj.OnSpawn();
            instance.prefabLookup.Add(obj, prefab);
            return obj;
        }
        else
        {
            Debug.LogError("Object pool not created for prefab " + prefab.name + "!", prefab.gameObject);
            T obj = (T)Object.Instantiate(prefab, position, rotation);
            obj.OnInstantiate();
            obj.OnSpawn();
            return obj;
        }
    }
    public static T Spawn<T>(T prefab, Vector3 position) where T : PooledMonoBehaviour
    {
        return Spawn(prefab, position, Quaternion.identity);
    }
    public static T Spawn<T>(T prefab) where T : PooledMonoBehaviour
    {
        return Spawn(prefab, Vector3.zero, Quaternion.identity);
    }

    public static void Recycle<T>(T obj) where T : PooledMonoBehaviour
    {
        if ((instance == null) || (obj == null)) return;

        if (instance.prefabLookup.ContainsKey(obj))
        {
            instance.objectLookup[instance.prefabLookup[obj]].Add(obj);
            instance.prefabLookup.Remove(obj);
            obj.transform.SetParent(instance.transform, false); // fgsfds?
            obj.OnRecycle();
            obj.gameObject.SetActive(false);
        }
        else
        {
            Object.Destroy(obj.gameObject);
        }
    }

    public static int Count<T>(T prefab) where T : PooledMonoBehaviour
    {
        if (instance == null) return 0;
        if (instance.objectLookup.ContainsKey(prefab))
            return instance.objectLookup[prefab].Count;
        else
            return 0;
    }

    public static ObjectPool instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            GameObject obj = GameObject.FindWithTag("RecycleBin");
            if (obj == null)
            {
                obj = new GameObject("_RecycleBin");
                obj.tag = "RecycleBin";
            }

            obj.transform.localPosition = Vector3.zero;
            _instance = obj.GetComponent<ObjectPool>();
            if (_instance == null)
            {
                _instance = obj.AddComponent<ObjectPool>();
            }
            Debug.Log ("Returning ObjectPool instance : IsNull[" + (_instance == null).ToString () + "]");
            return _instance;
        }
    }
}

public static class ObjectPoolExtensions
{
    public static void CreatePool<T>(this T prefab) where T : PooledMonoBehaviour
    {
        ObjectPool.CreatePool(prefab);
    }
    public static void RemovePool<T>(this T prefab) where T : PooledMonoBehaviour
    {
        ObjectPool.RemovePool(prefab);
    }
    public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : PooledMonoBehaviour
    {
        return (T)ObjectPool.Spawn(prefab, position, rotation);
    }
    public static T Spawn<T>(this T prefab, Vector3 position) where T : PooledMonoBehaviour
    {
        return (T)ObjectPool.Spawn(prefab, position, Quaternion.identity);
    }
    public static T Spawn<T>(this T prefab) where T : PooledMonoBehaviour
    {
        return (T)ObjectPool.Spawn(prefab, Vector3.zero, Quaternion.identity);
    }

    public static void Recycle<T>(this T obj) where T : PooledMonoBehaviour
    {
        ObjectPool.Recycle(obj);
    }

    public static int Count<T>(T prefab) where T : PooledMonoBehaviour
    {
        return ObjectPool.Count(prefab);
    }
}
