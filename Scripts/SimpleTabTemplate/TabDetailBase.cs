using System;
using UnityEngine;

public class TabDetailBase : MonoBehaviour
{
    public ITabInfo info;

    public int TabIndex { get; set; }

    public virtual bool IsLoading { get; protected set; } = true;

    public Action<TabDetailBase, GameObject> OnLoadCompleted { get; set; }

    public virtual void StartLoadAsset(ITabInfo info)
    {
        this.info = info;
    }

    public virtual void OnActivated() { }

    public virtual void OnDeactivated() { }
}
