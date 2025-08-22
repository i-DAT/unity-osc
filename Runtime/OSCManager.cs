using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class OscManager : IDisposable
{
    public int port = 8000;
    public Func<IPEndPoint, OscClient> OnConnect;

    public readonly ConcurrentQueue<OscPacket> sendQueue = new();
    public readonly ConcurrentQueue<OscPacket> recvQueue = new();

    Thread send;
    Thread recv;
    Thread broadcast;

    readonly Dictionary<IPEndPoint, OscClient> clients = new();

    public void Start()
    {
        send = new Thread(Send);
        recv = new Thread(Recv);
        broadcast = new Thread(Broadcast);
        send.Start();
        recv.Start();
        broadcast.Start();
    }

    public void Dispose()
    {
        send.Abort();
        recv.Abort();
        broadcast.Abort();
    }

    public void Update()
    {
        while (recvQueue.TryDequeue(out var packet))
        {
            if (!clients.ContainsKey(packet.EndPoint)) clients[packet.EndPoint] = OnConnect(packet.EndPoint);
            var client = clients[packet.EndPoint];
            var method = client.GetType().GetMethod(packet.Message.Address);
            if (method != null) method.Invoke(client, packet.Message.Args);
            else client.OnMessage(packet.Message);
        }
    }

    void Send()
    {
        using var client = new UdpClient();
        while (true)
        {
            while (sendQueue.TryDequeue(out var packet))
            {
                var data = OscBuilder.BuildMessage(packet.Message);
                client.Send(data, data.Length, packet.EndPoint);
            }
            Thread.Sleep(1);
        }
    }
    void Recv()
    {
        using var client = new UdpClient(port);
        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            var data = client.Receive(ref endpoint);
            var msg = OscParser.ParseMessage(data);
            recvQueue.Enqueue(new OscPacket(new IPEndPoint(endpoint.Address, endpoint.Port), msg));
        }
    }

    void Broadcast()
    {
        // Loop send the host name, IP, and port to the broadcast IP.
        using var client = new UdpClient("239.255.255.250", 4001);
        var hostName = Dns.GetHostName();
        var hostIp = Dns.GetHostEntry(hostName).AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?
                    .ToString();
        while (true)
        {
            byte[] data = OscBuilder.BuildMessage(new OscMessage("/host", new dynamic[] { hostName, hostIp, port }));
            client.Send(data, data.Length);
            Thread.Sleep(1000);
        }
    }
}

public abstract class OscClient
{
    public IPEndPoint EndPoint { get; set; }
    public ConcurrentQueue<OscPacket> SendQueue { get; set; }

    public OscClient(IPEndPoint endpoint, ConcurrentQueue<OscPacket> sendQueue)
    {
        EndPoint = endpoint;
        SendQueue = sendQueue;
    }

    public abstract void OnMessage(OscMessage message);
}

public interface IOscClientFactory
{
    OscClient CreateClient(IPEndPoint endpoint);
}

public class OscPacket
{
    public IPEndPoint EndPoint { get; }
    public OscMessage Message { get; }

    public OscPacket(IPEndPoint endpoint, OscMessage message)
    {
        EndPoint = endpoint;
        Message = message;
    }
}

public class OscMessage
{
    public string Address { get; }
    public object[] Args { get; }

    public OscMessage(string address, object[] args)
    {
        Address = address;
        Args = args;
    }

    public override string ToString()
    {
        return $"{Address} {string.Join(", ", Args)}";
    }
}
