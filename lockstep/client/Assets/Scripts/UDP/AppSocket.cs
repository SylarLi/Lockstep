using System;
using System.Collections.Generic;

/// <summary>
/// Application Level Socket
/// 心跳发送，统计延迟
/// </summary>
public class AppSocket : USocket
{
    public Dictionary<PacketType, Action<Packet>> packetHandler;

    public double latency;

    private float tick;

    public AppSocket(uint id) : base(id)
    {
        latency = NetConfig.TPF * 2;
        tick = 0;
        packetHandler = new Dictionary<PacketType, Action<Packet>>();
    }

    public override void Close()
    {
        base.Close();
        latency = NetConfig.TPF * 2;
        tick = 0;
        packetHandler = new Dictionary<PacketType, Action<Packet>>();
    }

    protected override void OnPacketReceived(Packet packet)
    {
        base.OnPacketReceived(packet);
        if (netState == NetState.Connected)
        {
            if (packetHandler.ContainsKey(packet.type))
            {
                packetHandler[packet.type](packet);
            }
        }
    }

    protected override void OnPacketArrived(Packet packet)
    {
        base.OnPacketArrived(packet);
        if (netState == NetState.Connected &&
            (packet.type == PacketType.HeartBeat || packet.type == PacketType.LockStep))
        {
            double newRTT = (DateTime.Now - packet.sendTime).TotalSeconds;
            latency = latency == 0 ? newRTT : (latency * 0.9f + newRTT * 0.1f);
        }
    }

    protected override void OnPacketLoss(Packet packet)
    {
        base.OnPacketLoss(packet);
        if (packet.type == PacketType.HeartBeat ||
            packet.type == PacketType.LockStep)
        {
            latency = latency == 0 ? NetConfig.LossRTT : (latency * 0.9f + NetConfig.LossRTT * 0.1f);
        }
        if (packet.type == PacketType.Message)
        {
            Send(packet.data, packet.type);
        }
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (netState == NetState.Connected)
        {
            if ((tick += deltaTime) > NetConfig.BeatInterval)
            {
                tick = 0;
                Send(null, PacketType.HeartBeat);
            }
        }
    }
}
