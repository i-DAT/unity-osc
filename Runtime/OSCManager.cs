using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class OSCManager : MonoBehaviour
{
    public int port = 8000;

    ConcurrentQueue<OSCMessage> queue;
    Thread listener;

    Dictionary<string, Action<OSCMessage>> handlers;
    public Action<OSCMessage> defaultHandler;


    void Awake()
    {
        queue = new ConcurrentQueue<OSCMessage>();
        handlers = new Dictionary<string, Action<OSCMessage>>();
        defaultHandler = DefaultHandler;

        // Start the listener thread to parse OSC messages. This will be aborted on cleanup.
        listener = new Thread(Listen);
        listener.IsBackground = true;
        listener.Start();
    }

    void OnDestroy()
    {
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
