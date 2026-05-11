using System.Net;

namespace ReliableUdp;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "server")
            await RunServer(int.Parse(args.ElementAtOrDefault(1) ?? "9000"));
        else if (args.Length > 0 && args[0] == "client")
            await RunClient(int.Parse(args.ElementAtOrDefault(1) ?? "9000"));
        else
        {
            Console.WriteLine("=== Reliable UDP Protocol Demo ===\n");
            Console.WriteLine("Usage: dotnet run server [port]   OR   dotnet run client [port]\n");
            Console.WriteLine("Running both locally (server on 9000)...\n");

            var serverDone = new TaskCompletionSource<bool>();
            _ = Task.Run(() => RunServer(9000, serverDone));

            await Task.Delay(300); // give server time to bind

            await RunClient(9000);

            await Task.Delay(300);
            serverDone.TrySetResult(true);
            Console.WriteLine("\nDemo finished.");
        }
    }

    static async Task RunServer(int port, TaskCompletionSource<bool>? done = null)
    {
        using var socket = new RudpSocket(port);
        Console.WriteLine($"[Server] Bound to {socket.LocalEndPoint}");

        int receivedCount = 0;
        int connCount = 0;

        socket.OnNewConnection += conn =>
        {
            int id = ++connCount;
            Console.WriteLine($"[Server] New connection #{id} from {conn.PeerEndPoint} (connId={conn.ConnectionId})");

            conn.OnDataReceived += data =>
            {
                string text = System.Text.Encoding.UTF8.GetString(data);
                receivedCount++;
                Console.WriteLine($"[Server] Conn#{id} received #{receivedCount}: '{text}'");

                // Echo back
                if (conn.Send(data))
                    Console.WriteLine($"[Server] Conn#{id} echoed back");
                else
                    Console.WriteLine($"[Server] Conn#{id} echo FAILED (state={conn.State})");
            };

            conn.OnClosed += () =>
            {
                Console.WriteLine($"[Server] Conn#{id} closed");
            };

            conn.OnPeerClosing += () =>
            {
                Console.WriteLine($"[Server] Conn#{id} peer closing — sending FIN");
                conn.Close();
            };
        };

        socket.Start();

        // Run until signaled
        if (done != null)
            await done.Task;
        else
            await Task.Delay(Timeout.Infinite); // run forever

        socket.Stop();
    }

    static async Task RunClient(int serverPort)
    {
        using var socket = new RudpSocket();
        socket.Start();

        var serverEp = new IPEndPoint(IPAddress.Loopback, serverPort);
        var conn = socket.Connect(serverEp);

        // Wait for handshake
        var connected = new TaskCompletionSource<bool>();
        var allEchoed = new TaskCompletionSource<bool>();
        int sent = 0;
        int echoed = 0;

        conn.OnDataReceived += data =>
        {
            string text = System.Text.Encoding.UTF8.GetString(data);
            echoed++;
            Console.WriteLine($"  [Client] Got echo #{echoed}: '{text}'");
            if (echoed >= sent)
                allEchoed.TrySetResult(true);
        };

        conn.OnClosed += () =>
        {
            Console.WriteLine("  [Client] Connection closed by peer");
        };

        // Poll state until established
        long deadline = Environment.TickCount64 + 5000;
        while (conn.State != RudpConnState.Established)
        {
            if (Environment.TickCount64 > deadline)
            {
                Console.WriteLine("  [Client] Connection timeout! State=" + conn.State);
                return;
            }
            await Task.Delay(10);
        }
        Console.WriteLine("  [Client] Connected!\n");

        // Send 10 numbered messages
        for (int i = 0; i < 10; i++)
        {
            string msg = $"Hello_World_{i:D3}_padding_to_make_packet_larger_and_more_interesting_xxxxxxxxxxxxxxxxxxxx";
            Console.WriteLine($"  [Client] Sending #{i + 1}: '{msg.Substring(0, Math.Min(30, msg.Length))}...' ({msg.Length}B)");
            if (conn.Send(System.Text.Encoding.UTF8.GetBytes(msg)))
                sent++;
            else
                Console.WriteLine($"  [Client] Send failed!");
            await Task.Delay(50);
        }

        // Wait for all echoes (with timeout)
        var timeout = Task.Delay(8000);
        var result = await Task.WhenAny(allEchoed.Task, timeout);
        if (result == timeout)
            Console.WriteLine($"\n  [Client] TIMEOUT — got {echoed}/{sent} echoes");
        else
            Console.WriteLine($"\n  [Client] SUCCESS — all {sent} messages echoed!");

        // Graceful close
        Console.WriteLine("  [Client] Closing...");
        conn.Close();
        await Task.Delay(500);
        Console.WriteLine($"  [Client] Final state: {conn.State}");

        socket.Stop();
    }
}
