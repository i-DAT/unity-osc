using System;

public class OscParser
{
    public static bool IsLittleEndian { set; get; } = false;

    public static OscMessage ParseMessage(byte[] packet)
    {
        // An OSC message consists of an address string, a format string, then any number of arguments.

        // The address string must start with a '/' to be valid. 
        var index = 0;
        var address = ParseString(packet, ref index);
        if (!address.StartsWith("/")) throw new FormatException("Invalid message address");
        address = address[1..];

        // The format string must start with a ',' to be valid.
        var tag = ParseString(packet, ref index);
        if (!tag.StartsWith(",")) throw new FormatException("Missing message type tag");

        // For each character of the format string (except the leading ',') dispatch to a parser function
        // based on its type and build an array of the results.
        var args = new dynamic[tag.Length - 1];
        for (int i = 1; i < tag.Length; i++)
        {
            args[i - 1] = tag[i] switch
            {
                'i' => ParseInt(packet, ref index),
                'f' => ParseFloat(packet, ref index),
                's' => ParseString(packet, ref index),
                _ => throw new FormatException($"Invalid tag type {tag[i]}"),
            };
        }

        if (index != packet.Length) throw new FormatException("Extra data in packet buffer");

        return new OscMessage(address, args);
    }

    public static int ParseInt(byte[] packet, ref int index)
    {
        // Read 4 bytes of data, flipping for endianess, and convert to an int32.
        Span<byte> data = stackalloc byte[4];
        packet.AsSpan(index, 4).CopyTo(data);
        index += 4;
        if (BitConverter.IsLittleEndian != IsLittleEndian) data.Reverse();
        return BitConverter.ToInt32(data);
    }

    public static float ParseFloat(byte[] packet, ref int index)
    {
        // Read 4 bytes of data, flipping for endianess, and convert to a float32.
        Span<byte> data = stackalloc byte[4];
        packet.AsSpan(index, 4).CopyTo(data);
        index += 4;
        if (BitConverter.IsLittleEndian != IsLittleEndian) data.Reverse();
        return BitConverter.ToSingle(data);
    }

    public static string ParseString(byte[] packet, ref int index)
    {
        // Read until a null byte.
        var start = index;
        while (index < packet.Length && packet[index] != 0) index++;
        string result = System.Text.Encoding.ASCII.GetString(packet, start, index - start);
        // Skip null terminator
        index += 1;
        var total = index - start;
        // Skip such that the total bytes read (including null terminator) has 4-byte alignment.
        index += (4 - (total % 4)) % 4;
        return result;
    }
}

