using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


public class DemoShowUtc : MonoBehaviour
{
    NtpUtcNowSynchronizer er;
    
    public Button btn;
    public Button btn_sync;


    async void Start()
    {
        er = new NtpUtcNowSynchronizer("time.cloudflare.com", "ntp.aliyun.com", "ntp.tencent.com");
        er.TimeoutMilliseconds = 100;
        er.Sync();
        er.OnNtpTimeSynchronized += (ret) => 
        {
            Debug.Log($"ntp server time synchronized! server:{ret.server}");
        };

        btn.onClick.AddListener(() => {

            Debug.Log($"utc client utc: {DateTime.UtcNow}");
            Debug.Log($"ntp server utc: {er.NtpUtcNow}");
        });

        btn_sync.onClick.AddListener(() => {
            er.Sync();
        });
    }
}
