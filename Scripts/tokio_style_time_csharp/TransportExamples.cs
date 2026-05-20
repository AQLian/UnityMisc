using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;

public class FlushExample : MonoBehaviour
{
    NetworkDriver _driver;
    NativeArray<NetworkConnection> _conn;
    JobHandle _handle;

    void Start()
    {
        _driver = NetworkDriver.Create();
        _conn = new NativeArray<NetworkConnection>(1, Allocator.Persistent);
        _conn[0] = _driver.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(7777));
    }

    void Update()
    {
        _handle.Complete();

        // recv + process + queue send + flush → all in one chain, same frame
        _handle = _driver.ScheduleUpdate();
        _handle = new ProcessJob { Driver = _driver, Conn = _conn }.Schedule(_handle);
        _handle = _driver.ScheduleFlushSend(_handle);   // <── key line
    }

    void OnDestroy()
    {
        _handle.Complete();
        if (_driver.IsCreated) { _driver.Dispose(); _conn.Dispose(); }
    }
}

struct ProcessJob : IJob
{
    public NetworkDriver Driver;
    public NativeArray<NetworkConnection> Conn;

    public void Execute()
    {
        DataStreamReader reader;
        NetworkEvent.Type evt;
        while ((evt = Conn[0].PopEvent(Driver, out reader)) != NetworkEvent.Type.Empty)
        {
            if (evt == NetworkEvent.Type.Data)
            {
                int val = reader.ReadInt();
                Driver.BeginSend(Conn[0], out var writer);
                writer.WriteInt(val + 1);
                Driver.EndSend(writer);
            }
        }
    }
}
