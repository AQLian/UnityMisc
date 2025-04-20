using AOT;
using System.Runtime.InteropServices;
using UnityEngine;

public class NativePluginExample
{
    // Delegate with explicit calling convention
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnNativeEventDelegate(int value);

    // Native function that takes a callback
    [DllImport("MyNativePlugin")]
    private static extern void RegisterCallback(OnNativeEventDelegate callback);

    // Callback method marked with MonoPInvokeCallback
    [MonoPInvokeCallback(typeof(OnNativeEventDelegate))]
    private static void OnNativeEvent(int value)
    {
        Debug.Log($"Received native event with value: {value}");
    }

    public static void Initialize()
    {
        RegisterCallback(OnNativeEvent);
    }
}