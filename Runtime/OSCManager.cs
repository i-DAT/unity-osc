using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using UnityEngine;

public class OSCManager : MonoBehaviour
{
    public int port = 8000;

    ConcurrentQueue<OSCMessage> queue;
    Thread sender;
    Thread listener;

    Dictionary<string, Action<OSCMessage>> handlers;
    public Action<OSCMessage> defaultHandler;


    void Awake()
    {
        queue = new ConcurrentQueue<OSCMessage>();
        handlers = new Dictionary<string, Action<OSCMessage>>();
        defaultHandler = DefaultHandler;


        // Start the sender thread to broadcast this IP.
        sender = new Thread(Send);
        sender.IsBackground = true;
        sender.Start();

        // Start the listener thread to parse OSC messages. This will be aborted on cleanup.
        listener = new Thread(Listen);
        listener.IsBackground = true;
        listener.Start();
    }

    void OnDestroy()
    {
        sender.Abort();
        listener.Abort();
    }

    void Update()
    {
        // When a new message is in the queue, try to find a handler for it based on the address string,
        // otherwise use the default handler function, and call it with the OSC message.
        while (queue.TryDequeue(out var msg))
        {
            (handlers.TryGetValue(msg.address, out var handler) ? handler : defaultHandler)(msg);
        }
    }

    public void Handle(string address, Action<OSCMessage> action)
    {
        handlers.Add(address, action);
    }

    void DefaultHandler(OSCMessage msg)
    {
        Debug.Log($"Unhandled message: {msg.address} {msg.args}");
    }

    void Send()
    {
        // Loop send the host name, IP, and port to the broadcast IP.
        using var client = new UdpClient("239.255.255.250", 4001);
        var hostName = Dns.GetHostName();
        var hostIp = Dns.GetHostEntry(hostName).AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?
                    .ToString();
        while (true)
        {
            byte[] data = OSCBuilder.BuildMessage(new OSCMessage("/host", new dynamic[] { hostName, hostIp, port }));
            client.Send(data, data.Length);
            Thread.Sleep(1000);
        }
    }

    void Listen()
    {
        // Loop and receive UDP datagrams.
        // Try to parse an OSC message from them and add it to the queue, logging any malformed packet errors.
        using var client = new UdpClient(port);
        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            byte[] data = client.Receive(ref endpoint);
            try
            {
                queue.Enqueue(OSCParser.ParseMessage(data));
            }
            catch (FormatException e)
            {
                Debug.Log(e);
            }
        }
    }
}
