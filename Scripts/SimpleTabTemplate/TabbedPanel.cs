using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

public  class TabbedPanel : MonoBehaviour, ITabInfo
{
    [SerializeField] protected ScrollRect scrollRect;
    [SerializeField] protected RectTransform detailRoot;
    [SerializeField] protected RectTransform loading;
    [SerializeField] protected TabData tabData;

    public List<TabButton> buttons;
    private Dictionary<int, GameObject> loaded = new();
    [SerializeField]
    private int m_activeTab = -1;
    public int ActiveTab => m_activeTab;
    private static MethodInfo cachedCallbackMethod;

    void Start() => BuildTabList();

    private void BuildTabList()
    {
        cachedCallbackMethod ??= typeof(TabbedPanel).GetMethod("PageCompltedAction", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int i = 0; i < tabData.tabs.Length; ++i)
        {
            int index = i; 
            GameObject go = Instantiate(tabData.tabButtonPrefab, scrollRect.content);
            var btn = go.GetOrAddComponent<TabButton>().Build(index, tabData.tabs[i].tabName, OnTabSelectHandler);
            buttons.Add(btn);
        }

        OnTabSelectHandler(0);
    }

    private void OnTabSelectHandler(int index)
    {
        if (index == m_activeTab) return;

        if (loaded.TryGetValue(m_activeTab, out GameObject prev))
        {
            prev.SetActive(false);
            if(prev.TryGetComponent<TabDetailBase>(out var prevDetail))
            {
                prevDetail.OnDeactivated();
            }
        }

        if (m_activeTab >= 0 && m_activeTab < buttons.Count)
        {
            buttons[m_activeTab].OnDeselect();
        }

        m_activeTab = index;
        if (m_activeTab >= 0 && m_activeTab < buttons.Count)
        {
            buttons[m_activeTab].OnSelect();
        }

        if (!loaded.TryGetValue(index, out GameObject content))
        {
            content = LoadDetailContent(tabData.tabs[index]);
            loaded[index] = content;
        }
        content.SetActive(true);

        if (content.TryGetComponent<TabDetailBase>(out var detail)) 
        {
            detail.TabIndex = m_activeTab;
            loading.gameObject.SetActive(detail.IsLoading);
            if (detail.IsLoading)
            {
                if (!CallbackRegistered(detail.OnLoadCompleted, cachedCallbackMethod, this))
                {
                    detail.OnLoadCompleted += PageCompltedAction;
                    detail.StartLoadAsset(this);
                }
            }
            else
            {
                detail.OnActivated();
            }
        }
    }


    private void PageCompltedAction(TabDetailBase b, GameObject content)
    {
        if(m_activeTab == b.TabIndex)
        {
            loading.gameObject.SetActive(false);
            b.OnActivated();
        }
    }

    protected virtual GameObject LoadDetailContent(TabData.Item item)
    {
        GameObject go = Instantiate(item.detailPrefab, detailRoot);
        go.SetActive(false);
        return go;
    }


    private bool CallbackRegistered(Delegate del, MethodInfo targetMethod, object target)
    {
        var invocationList = del?.GetInvocationList();
        if (invocationList == null) return false;
        foreach (var invocation in invocationList)
        {
            if (invocation.Method == targetMethod && invocation.Target == target)
            {
                return true;
            }
        }
        return false;
    }
}

