using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.Networking.Types;

public class QueueMgr : MonoBehaviour
{
    public GameObject prefab;
    public int initCount;
    public GameObject parent;

    public Queue<(GameObject, float)> items = new();
    public QueueItem runningItem;
    public List<float> lifes;

    // Start is called before the first frame update
    void Start()
    {
        for(var i = 0; i < initCount; i++)
        {
            EnqueueItem(prefab, 1);
        }
    }

    private void Update()
    {
        lifes.Clear();
        foreach(var i in items)
        {
            lifes.Add(i.Item2);
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            EnqueueItem(prefab, UnityEngine.Random.Range(1f, 5f));
        }
    }

    void EnqueueItem(GameObject prefab, float life)
    {
        items.Enqueue((prefab, life));

        if (runningItem == null)
        {
            runningItem = NextItem();
        }
    }

    QueueItem NextItem()
    {
        var (prefab, life) = items.Dequeue();
        var ins = GameObject.Instantiate(prefab).GetComponent<QueueItem>().Init(life);
        ins.transform.SetParent(parent.transform, false);
        ins.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Random.ColorHSV();
        ins.onDestoryed += g => {
            if(items.Count > 0)
            {
                runningItem = NextItem();
            }
        };
        return ins;
    }
}
