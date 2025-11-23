public static class UnityUtil
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (comp == null)
        {
            comp = go.AddComponent<T>();
        }
        return comp;
    }

    
    static string KeepOnlyChinese(string input)
    {
        return Regex.Replace(input, @"[^\u4E00-\u9FFF]", "");
    }
}