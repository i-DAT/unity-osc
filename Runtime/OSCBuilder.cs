using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class OSCBuilder
{
    public static bool IsLittleEndian { set; get; } = false;

    public static byte[] BuildMessage(OSCMessage message)
    {
        var stream = new MemoryStream();

        Write(stream, message.address);
        var args = new char[message.args.Length + 1];
        args[0] = ',';
        for (int i = 0; i < message.args.Length; i++) args[i + 1] = message.args[i] switch
        {
            int => 'i',
            float => 'f',
            string => 's',
            _ => throw new ArgumentException("Invalid argument type")
        };
        Write(stream, new string(args));
        Array.ForEach(message.args, arg => Write(stream, arg));

        return stream.ToArray();
    }

    static void Write(Stream stream, int value)
    {
        var data = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian != IsLittleEndian) data = data.Reverse().ToArray();
        stream.Write(data);
    }

    static void Write(Stream stream, float value)
    {
        var data = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian != IsLittleEndian) data = data.Reverse().ToArray();
        stream.Write(data);
    }

    static void Write(Stream stream, string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);
        var n = bytes.Length + 1;
        stream.Write(bytes);
        stream.WriteByte(0);
        for (int i = 0; i < (4 - (n % 4)) % 4; i++) stream.WriteByte(0);
    }
}

