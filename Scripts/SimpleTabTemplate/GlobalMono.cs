using System.Collections.Generic;
using UnityEngine;
// normally when startcoroutine over a gamobject that 
// gameobject SetActive(false) action will stop all started coroutine
// using global mono behaviour with dontdestroyonload and StartCoroutine it will keep running
[DisallowMultipleComponent]
public class GlobalMono : MonoBehaviour
{
    private static GlobalMono s_instance;

    public static GlobalMono Instance
    {
        get
        {
            if(s_instance!=null) return s_instance;   
        
            var find = FindObjectOfType<GlobalMono>();
            if (find == null)
            {
                var go = new GameObject("GlobalMono");
                s_instance = go.AddComponent<GlobalMono>();
            }
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(this);
    }
}

