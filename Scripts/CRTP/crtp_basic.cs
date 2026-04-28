using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEngine.Assertions;

public abstract class MyBase : IDisposable
{
    protected bool pooled { get; set; }

    protected abstract void Acquire();

    protected abstract void Return();

    public abstract void Dispose();
}

public class Base<T> : MyBase where T : Base<T> , new() 
{
    private static ObjectPool<T> s_Pooled = new ObjectPool<T>(createFunc: () => new T());

    public static T GetPooled()
    {
        var inst = s_Pooled.Get();
        inst.pooled = true;
        inst.Acquire();
        return inst;
    }

    protected override void Acquire()
    {
    }

    protected override void Return()
    {
    }

    public override void Dispose()
    {
        if (this.pooled)
        {
            Return();
            s_Pooled.Release((T)this);
            this.pooled = false;
        }
    }

    public static int PooledCount => s_Pooled.CountInactive;
    public static int CountActive => s_Pooled.CountActive;
}

public class ChangeEvt : Base<ChangeEvt>
{
}

public class GenericBase
{
    static long s_NextId;
    internal static long RegisterCalled(Type t)
    {
        s_NextId++;
        return s_NextId;
    }
}

public class GenericOver<T> : GenericBase
{
    public static long TypeId = GenericBase.RegisterCalled(typeof(T));
    public long Id => TypeId;
}

public class CRTP : MonoBehaviour
{
    void Awake()
    {
        for (var i = 0; i< 10; i++)
        {
            using var evt = ChangeEvt.GetPooled();
        }
        Assert.AreEqual(ChangeEvt.PooledCount, 1, "may be not return object after used");
    }
}
