using System;
using System.Reflection;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class MyClass
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}

public class MethodCallBenchmark
{
    private MyClass _instance;
    private MethodInfo _cachedMethodInfo;
    private Func<int, int, int> _cachedDelegate;
    private Func<int, int, int> _expressionDelegate;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new MyClass();

        // Cache MethodInfo once
        _cachedMethodInfo = typeof(MyClass).GetMethod("Add");

        // Cache Delegate once (via CreateDelegate)
        _cachedDelegate = (Func<int, int, int>)Delegate.CreateDelegate(
            typeof(Func<int, int, int>),
            _instance,
            _cachedMethodInfo
        );

        // Cache Expression Delegate once
        var paramA = Expression.Parameter(typeof(int), "a");
        var paramB = Expression.Parameter(typeof(int), "b");
        var call = Expression.Call(
            Expression.Constant(_instance),
            _cachedMethodInfo,
            paramA,
            paramB
        );
        _expressionDelegate = Expression.Lambda<Func<int, int, int>>(
            call,
            paramA,
            paramB
        ).Compile();
    }

    [Benchmark(Baseline = true)]
    public int DirectCall()
    {
        return _instance.Add(5, 3); // Baseline: direct method call
    }

    [Benchmark]
    public int CachedMethodInfoInvoke()
    {
        return (int)_cachedMethodInfo.Invoke(_instance, new object[] { 5, 3 });
    }

    [Benchmark]
    public int CachedDelegateCall()
    {
        return _cachedDelegate(5, 3);
    }

    [Benchmark]
    public int ExpressionDelegateCall()
    {
        return _expressionDelegate(5, 3);
    }
}

class Program
{
    static void Main()
    {
        var summary = BenchmarkRunner.Run<MethodCallBenchmark>();
    }
}