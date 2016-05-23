using System;
using System.IO;
using UnityEngine;

public class Packet : Pipe
{
    // --------------- Serialized --------------- //

    public PacketType type;

    public uint id;

    public RUShortInt sequence;

    public RUShortInt ack;

    public uint ackbit;

    public byte[] data;

    // -------------- UnSerialized -------------- //

    public DateTime sendTime;

    public bool arrived;

    public Packet()
    {
        type = PacketType.Message;
    }

    public override void Parse(BinaryReader reader)
    {
        type = (PacketType)reader.ReadByte();
        id = reader.ReadUInt32();
        sequence = reader.ReadByte();
        ack = reader.ReadByte();
        ackbit = reader.ReadUInt32();
        long dataLen = reader.BaseStream.Length - reader.BaseStream.Position;
        if (dataLen > 0)
        {
            data = new byte[dataLen];
            reader.Read(data, 0, data.Length);
        }
    }

    public override void ToBytes(BinaryWriter writer)
    {
        writer.Write((byte)type);
        writer.Write(id);
        writer.Write(sequence.value);
        writer.Write(ack.value);
        writer.Write(ackbit);
        if (data != null)
        {
            writer.Write(data);
        }
    }
}
