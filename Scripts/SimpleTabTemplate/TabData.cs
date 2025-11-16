// TabData.cs
using UnityEngine;

[CreateAssetMenu(menuName = "UI/TabData")]
public class TabData : ScriptableObject
{
    [System.Serializable]
    public class Item
    {
        public string tabName;
        public GameObject detailPrefab;
    }

    public Item[] tabs = new Item[0];
    public GameObject tabButtonPrefab;
}
