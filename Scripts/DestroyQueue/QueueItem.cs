using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueueItem : MonoBehaviour
{
    public float _life;
    public float _max = 3f;
    public float _started;

    // Start is called before the first frame update
    void Start()
    {
        _started = Time.realtimeSinceStartup;
    }

    internal QueueItem Init(float f)
    {
        _max = f;
        return this;
    } 

    // Update is called once per frame
    void Update()
    {
        if(_life - _started >= _max)
        {
            DetroySelf();
        }
        _life = Time.realtimeSinceStartup;
    }

    public Action<QueueItem> onDestoryed;
    public Action<QueueItem> onDestroyedIm;

    private void DetroySelf()
    {
        onDestoryed?.Invoke(this);
        GameObject.DestroyImmediate(gameObject);
    }
}
