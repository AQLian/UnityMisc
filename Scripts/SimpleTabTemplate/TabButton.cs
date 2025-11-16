using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabButton : MonoBehaviour
{
    private TextMeshProUGUI label;
    private GameObject select;
    private int tabIndex;
    private System.Action<int> onClick;

    void Awake()
    {
        label = transform.GetComponentInChildren<TextMeshProUGUI>();
        select = transform.Find("Select").gameObject;
    }

    public TabButton Build(int index, string name, System.Action<int> callback)
    {
        tabIndex = index;
        label.text = name;
        onClick = callback;
        GetComponent<Button>().onClick.AddListener(() => { onClick(index); });
        return this;
    }

    public void OnSelect()
    {
        select.SetActive(true);
    }

    public void OnDeselect()
    {
        select.SetActive(false);
    }
}

