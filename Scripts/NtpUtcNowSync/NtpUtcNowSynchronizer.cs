using System;
using UnityEngine;
using StopWatch = System.Diagnostics.Stopwatch;

/// <summary>
/// Network Time Protocol 
/// 获取真实的UtcNow，避免玩家自行修改本机时间导致的UtcNow获取错误
/// </summary>
public class NtpUtcNowSynchronizer
{
    public bool Synchornized { get; private set; }

    public DateTime LastSyncUtcNow {  get; private set; }

    public DateTime NtpUtcNow => Synchornized ? LastSyncUtcNow.AddMilliseconds(localWatch.ElapsedMilliseconds) : DateTime.UtcNow;

    public Action<AsyncNtpClient.NtpResult> OnNtpTimeSynchronized;

    private StopWatch localWatch;

    private string[] m_servers;

    public int TimeoutMilliseconds { get; set; } = 3000;

    public NtpUtcNowSynchronizer(params string[] servers)
    {
        m_servers= servers;
    }

    public void Sync()
    {
        DoSync();
    }

    async void DoSync()
    {
        try
        {
            var ret = await AsyncNtpClient.GetFirst(m_servers, TimeoutMilliseconds);
            LastSyncUtcNow = ret.Item1.UtcTime.AddMilliseconds(ret.Item1.Rtt.Milliseconds);
            Synchornized = true;
            localWatch = localWatch ?? new();
            localWatch.Restart();
            OnNtpTimeSynchronized?.Invoke(ret.Item1);
        }
        catch(System.TimeoutException)
        {
            Debug.Log($"{nameof(NtpUtcNowSynchronizer)} failed, timeout!");
        }
        catch(Exception e)
        {
            Debug.Log($"{nameof(NtpUtcNowSynchronizer)} failed : {e}");
        }
    }
}