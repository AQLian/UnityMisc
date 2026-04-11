using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using UnityEngine;
using StopWatch = System.Diagnostics.Stopwatch;

// some common Ntp svr 
// "ntp.aliyun.com"
// "ntp.tencent.com"
// "time.cloudflare.com"
// "time.google.com"
// "cn.ntp.org.cn"
public class AsyncNtpClient
{
    public struct NtpResult
    {
        public string server;
        public DateTime UtcTime;   // server transmit utcnow
        public TimeSpan ProcessOffset;    // server process Time
        public TimeSpan Rtt;       // rtt
    }

    public static readonly DateTime NtpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public string ntpServer;
    public int timeoutMilliseconds;

    // help functions
    public async static Task<(NtpResult,int)> GetFirst(string[] servers, int timeoutMilliseconds = 3000)
    {
        if (servers == null) {  throw new ArgumentNullException(nameof(servers)); }
        var clients = new List<Task<NtpResult>>(servers.Length);
        foreach(var s in servers)
        {
            clients.Add(new AsyncNtpClient(s, timeoutMilliseconds).GetUtcNow());
        }
        Task<NtpResult> ret = default;
        List<Task<NtpResult>> failed = new ();
        var cnt = clients.Count;
        while (cnt> 0)
        {
            ret = await Task.WhenAny(clients);
            if (!ret.IsCompletedSuccessfully)
            {
                failed.Add(ret);
                clients.Remove(ret);
                cnt = clients.Count;
            }
            else { break; }
        }
        return (await ret, failed.Count);
    }

    public async static Task<NtpResult[]> GetAll(string[] servers, int timeoutMilliseconds = 3000)
    {
        if (servers == null) { throw new ArgumentNullException(nameof(servers)); }
        var clients = new List<Task<NtpResult>>(servers.Length);
        foreach (var s in servers)
        {
            clients.Add(new AsyncNtpClient(s, timeoutMilliseconds).GetUtcNow());
        }
        return await Task.WhenAll(clients).ConfigureAwait(false);
    }

    public AsyncNtpClient(string ntpServer, int timeoutMilliseconds)
    {
        this.ntpServer = ntpServer;
        this.timeoutMilliseconds = timeoutMilliseconds;
    }

    /// <summary>
    /// Asynchronously gets the current UTC time from ntp.aliyun.com using the NTP protocol.
    /// </summary>
    /// <param name="timeoutMilliseconds">Timeout in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>UTC DateTime as reported by the server.</returns>
    public async Task<NtpResult> GetUtcNow(CancellationToken cancellationToken = default)
    {
        const int ntpPort = 123;
        // Prepare NTP request packet (48 bytes, first byte = 0x1B)
        byte[] ntpData = new byte[48];
        ntpData[0] = 0x1B; // LI=0, VN=3, Mode=3 (client)

        // Resolve DNS asynchronously
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(ntpServer).ConfigureAwait(false);
        if (addresses.Length == 0)
            throw new InvalidOperationException($"No IP addresses found for {ntpServer}.");

        var remoteEndPoint = new IPEndPoint(addresses[0], ntpPort);

        var watch = StopWatch.StartNew();
        using var udpClient = new UdpClient();
        // Connect to the remote endpoint to simplify Send/Receive usage
        udpClient.Connect(remoteEndPoint);

        // Create a timeout token linked with the user's cancellation token
        using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token, cancellationToken);

        try
        {
            // Send the request
            await udpClient.SendAsync(ntpData, ntpData.Length).ConfigureAwait(false);

            // Receive the response with cancellation support
            var result = await udpClient.ReceiveAsync(linkedCts.Token).ConfigureAwait(false);
            byte[] response = result.Buffer;

            // Parse the transmit timestamp (bytes 40-47, big-endian)
            DateTime utcTimeSvrReceive = ParseNtpTimestamp(response, 32); // Receive Timestamp (bytes 32-39)
            DateTime utcTimeSvrTransmit = ParseNtpTimestamp(response, 40); // Transmit Timestamp (bytes 40-47)
            var processTime = utcTimeSvrTransmit - utcTimeSvrReceive;
            var elapsed = watch.Elapsed;
            var rtt = elapsed - processTime;

            return new NtpResult { server = this.ntpServer, UtcTime = utcTimeSvrTransmit, ProcessOffset = processTime, Rtt = rtt };
        }
        catch (OperationCanceledException)
        {
            // If timeout caused the cancellation, throw a TimeoutException
            if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                throw new TimeoutException($"NTP request timed out after {timeoutMilliseconds} ms.");
            throw;
        }
    }

    // 辅助方法：解析NTP时间戳（大端序）
    private static DateTime ParseNtpTimestamp(byte[] buffer, int offset)
    {
        ulong intPart = 0;
        for (int i = 0; i < 4; i++)
            intPart = (intPart << 8) | buffer[offset + i];

        ulong fractPart = 0;
        for (int i = 4; i < 8; i++)
            fractPart = (fractPart << 8) | buffer[offset + i];

        double seconds = intPart + (fractPart / (double)0x100000000L);
        return NtpEpoch.AddSeconds(seconds);
    }
}


public static class UdpClientExtensions
{
    public static async Task<UdpReceiveResult> ReceiveAsync(this UdpClient udpClient, CancellationToken cancellationToken)
    {
        var receiveTask = udpClient.ReceiveAsync();
        using (cancellationToken.Register(() => udpClient.Close()))
        {
            try
            {
                return await receiveTask;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return default;
            }
        }
    }
}
