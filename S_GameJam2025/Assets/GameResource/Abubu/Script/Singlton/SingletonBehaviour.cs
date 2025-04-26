using UnityEngine;
public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    protected virtual void Awake()
    {
        if (Instance != null && !ReferenceEquals(Instance, this))
        {
            Destroy(gameObject);
            return;
        }
        Instance = this as T;
        DontDestroyOnLoad(gameObject);
        OnSingletonInit();
    }

    protected virtual void OnSingletonInit() { }

    public static T Instance { get; private set; }
}
