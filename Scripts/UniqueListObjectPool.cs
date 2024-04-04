using System;
using System.Collections.Generic;

public static class UniqueListObjectPool<T> where T : new()
{
    private static UniqueObjectPool<List<T>> s_PoolItems = new UniqueObjectPool<List<T>>(null, l=>l.Clear(), -1);

    public static List<T> Get()
    {
        return s_PoolItems.Get();
    }

    public static void Release(List<T> item)
    {
        s_PoolItems.Release(item);
    }
}