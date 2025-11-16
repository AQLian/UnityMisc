using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadSubTempalte : TabDetailBase
{
    public GameObject additionalRealObj;

    public override void StartLoadAsset(ITabInfo info)
    {
        base.StartLoadAsset(info);
        GlobalMono.Instance.StartCoroutine(DoLoading());
    }

    IEnumerator DoLoading()
    {
        yield return new WaitForSeconds(3f);
        IsLoading = false;
        additionalRealObj = new GameObject("XX");
        OnLoadCompleted(this, additionalRealObj);
        additionalRealObj.transform.SetParent(this.transform, false);
        if(info.ActiveTab == TabIndex)
        {
            OnActivated();
        }
        else
        {
            OnDeactivated();
        }
    }

    public override void OnActivated()
    {
    }

    public override void OnDeactivated()
    {
    }
}
