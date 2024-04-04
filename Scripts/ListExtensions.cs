using System;
using System.Collections.Generic;

public static class ListExtensions
{
    public static T RemoveLast<T>(this List<T> list) where T : new()
    {
        var listCount = list.Count;
        if (listCount > 0)
        {
            var last = list[listCount -1];
            list.RemoveAt(listCount - 1);
            return last;
        }

        return default(T);
    }

    public static void SwapRemove<T>(this List<T> list, int toRemove) where T : new()
    {
        var listCount = list.Count;
        if (listCount > 0 && toRemove < listCount)
        {
            var last = list[listCount - 1];
            var item = list[toRemove];
            list[listCount - 1] = item;
            list[toRemove] = last;
            RemoveLast(list);
        }
    }
}
