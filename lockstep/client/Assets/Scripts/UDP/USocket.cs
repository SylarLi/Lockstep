using Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// UDP
/// 控制状态: 连接和断开
/// 收发包
/// </summary>
public class USocket : EventDispatcher, INetTick
{
    private uint id;

    private UdpClient sc;

    private UdpClient rc;

    private IPEndPoint ep;

    private bool receiveBuild;

    private NetState state;

    private RUShortInt sequence;

    private RUShortInt ack;

    private uint ackbit;

    private Dictionary<byte, Packet> quantics;

    private List<Packet> cacheSend;

    private List<Packet> cacheReceived;

    private List<byte> cacheLost;

    private DateTime lastReceviedTime;

    private DateTime lastConnectTime;

    private float connectTimes;

    public LimitedQueue<Packet> sends;

    public LimitedQueue<Packet> receives;

    public USocket(uint id)
    {
        this.id = id;
        netState = NetState.None;
        sequence = 0;
        ack = 0;
        ackbit = 0;
        quantics = new Dictionary<byte, Packet>();
        cacheSend = new List<Packet>();
        cacheReceived = new List<Packet>();
        cacheLost = new List<byte>();
        lastConnectTime = lastReceviedTime = DateTime.MinValue;
        connectTimes = 0;
        sends = new LimitedQueue<Packet>(10);
        receives = new LimitedQueue<Packet>(10);
    }

    public NetState netState
    {
        get
        {
            return state;
        }
        set
        {
            if (state != value)
            {
                state = value;
                DispatchEvent(new SocketEvent(SocketEvent.StateChange));
            }
        }
    }

    public virtual void Connect(IPAddress serverIp, int serverPort, int clientPort)
    {
        if (netState == NetState.None)
        {
            try
            {
                if (sc == null)
                {
                    sc = new UdpClient(clientPort);
                    sc.Connect(serverIp, serverPort);
                }
                if (ep == null)
                {
                    ep = new IPEndPoint(serverIp, clientPort);
                }
                if (rc == null)
                {
                    rc = new UdpClient(ep);
                    rc.BeginReceive(new AsyncCallback(ReceiveCallBack), ep);
                }
            }
            catch (Exception ecp)
            {
                Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
            }
            netState = NetState.Connecting;
        }
    }

    public virtual void Send(byte[] data, PacketType type = PacketType.Message)
    {
        Packet packet = new Packet();
        packet.type = type;
        packet.id = id;
        packet.sequence = ++sequence;
        packet.ack = ack;
        packet.ackbit = ackbit;
        packet.data = data;
        cacheSend.Add(packet);
    }

    public virtual void Close()
    {
        netState = NetState.None;
        sequence = 0;
        ack = 0;
        ackbit = 0;
        quantics.Clear();
        cacheSend.Clear();
        cacheReceived.Clear();
        cacheLost.Clear();
        lastConnectTime = lastReceviedTime = DateTime.MinValue;
        connectTimes = 0;
        sends.Clear();
        receives.Clear();
        try
        {
            if (rc != null)
            {
                rc.Close();
            }
        }
        catch (Exception ecp)
        {
            Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
        }
        try
        {
            if (sc != null)
            {
                sc.Close();
            }
        }
        catch (Exception ecp)
        {
            Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
        }
        rc = null;
        sc = null;
        ep = null;
    }

    private void SendCallBack(IAsyncResult ar)
    {
        int length = 0;
        if (sc.Client.Connected)
        {
            try
            {
                length = sc.EndSend(ar);
            }
            catch (Exception ecp)
            {
                Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
            }
        }
    }

    private void SendPacket(Packet packet)
    {
        packet.sendTime = DateTime.Now;
        quantics.Add(packet.sequence.value, packet);
        if (packet.type != PacketType.HeartBeat)
        {
            sends.Enqueue(packet);
        }
        byte[] bytes = packet.ToBytes();
        if (bytes != null && bytes.Length > 0)
        {
            try
            {
                sc.BeginSend(bytes, bytes.Length, new AsyncCallback(SendCallBack), null);
            }
            catch (Exception ecp)
            {
                Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
            }
        }
    }

    private void ReceiveCallBack(IAsyncResult ar)
    {
        IPEndPoint e = (IPEndPoint)ar.AsyncState;
        if (rc.Client.Connected)
        {
            byte[] bytes = null;
            try
            {
                bytes = rc.EndReceive(ar, ref e);
                rc.BeginReceive(new AsyncCallback(ReceiveCallBack), e);
            }
            catch (Exception ecp)
            {
                Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
            }
            if (bytes != null)
            {
                try
                {
                    Packet packet = new Packet();
                    if (packet.Parse(bytes))
                    {
                        if (packet.id == id)
                        {
                            cacheReceived.Add(packet);
                        }
                        else
                        {
                            throw new InvalidOperationException("Receive Cross Packet: " + packet.id);
                        }
                    }
                }
                catch (Exception ecp)
                {
                    Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
                }
            }
        }
    }

    private void OnAck(Packet packet)
    {
        // 遍历处理超过32次收包未被应答的包
        foreach (byte seq in quantics.Keys)
        {
            if (packet.ack > seq && (packet.ack - seq).value >= 32)
            {
                cacheLost.Add(seq);
            }
        }
        foreach (byte seq in cacheLost)
        {
            Packet p = quantics[seq];
            quantics.Remove(seq);
            OnPacketLoss(p);
        }
        cacheLost.Clear();
        // 对已发包列表应用应答，移除已被应答的包
        for (int i = 0; i < 32; i++)
        {
            if ((packet.ackbit & (1 << i)) > 0)
            {
                byte seq = (packet.ack - i).value;
                if (quantics.ContainsKey(seq))
                {
                    Packet p = quantics[seq];
                    quantics.Remove(seq);
                    OnPacketArrived(p);
                }
            }
        }
        // 写收包应答
        if (packet.sequence > ack)
        {
            ackbit = ackbit << (int)(packet.sequence - ack).value;
            ackbit |= (1 << 0);
            ack = packet.sequence;
            OnPacketReceived(packet);
        }
        else
        {
            uint value = (uint)(1 << (int)(ack - packet.sequence).value);
            if ((ackbit & value) == 0)
            {
                // 如果收到的包是未被处理的
                ackbit |= value;
                OnPacketReceived(packet);
            }
            else
            {
                Debug.Log("Receive packet already handle : sequence " + packet.sequence);
            }
        }
    }

    /// <summary>
    /// 收包
    /// </summary>
    /// <param name="packet"></param>
    protected virtual void OnPacketReceived(Packet packet)
    {
        if (packet.type != PacketType.HeartBeat)
        {
            receives.Enqueue(packet);
        }
        if (netState == NetState.Connecting &&
            packet.type == PacketType.Connect)
        {
            netState = NetState.Connected;
        }
        if (netState == NetState.Connected)
        {
            lastReceviedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 确认发包已到达
    /// </summary>
    protected virtual void OnPacketArrived(Packet packet)
    {
        packet.arrived = true;
    }

    /// <summary>
    /// 确认发包超时未到达
    /// </summary>
    protected virtual void OnPacketLoss(Packet packet)
    {
        Debug.Log("packet loss : " + packet.type + " " + packet.sequence);
    }

    public virtual void Update(float deltaTime)
    {
        if (netState > NetState.None)
        {
            if (cacheReceived.Count > 0)
            {
                for (int i = 0; i < cacheReceived.Count; i++)
                {
                    OnAck(cacheReceived[i]);
                }
                cacheReceived.Clear();
            }
            if (cacheSend.Count > 0)
            {
                for (int i = 0; i < cacheSend.Count; i++)
                {
                    SendPacket(cacheSend[i]);
                }
                cacheSend.Clear();
            }
        }
        if (netState == NetState.Connecting)
        {
            if ((DateTime.Now - lastConnectTime).TotalSeconds >= NetConfig.ConnnectTimeout)
            {
                if (connectTimes < NetConfig.MaxConnectTimes)
                {
                    connectTimes++;
                    lastConnectTime = DateTime.Now;
                    Send(null, PacketType.Connect);
                }
                else
                {
                    Close();
                }
            }
        }
        else if (netState == NetState.Connected)
        {
            if ((DateTime.Now - lastReceviedTime).TotalSeconds >= NetConfig.BreakTimeout)
            {
                Close();
            }
        }
    }
}
