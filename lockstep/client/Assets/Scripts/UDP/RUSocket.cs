//using System;
//using System.Collections.Generic;
//using System.Net;
//using UnityEngine;

///// <summary>
///// Reliable Ordering Congestion avoidance UDP
///// </summary>
//public class RUSocket : USocket, INetTick
//{
//    private uint id;

//    private RUShortInt sequence;

//    private RUShortInt ack;

//    private uint ackbit;

//    private Dictionary<ushort, Packet> quantics;

//    private float mLatency;

//    private NetState mNetState;

//    private LimitedQueue<NetState> mNetStates;

//    private DateTime lastPacketReceviedTime;

//    private DateTime lastConnectTime;

//    private float connectTimes;

//    private LimitedQueue<Packet> mLastPackets;

//    public RUSocket(uint identity)
//    {
//        id = identity;
//        sequence = 0;
//        ack = 0;
//        ackbit = 0;
//        quantics = new Dictionary<ushort, Packet>();
//        mLatency = NetConfig.RTT_GOOD;
//        mNetState = NetState.None;
//        mNetStates = new LimitedQueue<NetState>(16);
//        lastPacketReceviedTime = DateTime.Now;
//        lastConnectTime = DateTime.Now;
//        connectTimes = 0;
//        mLastPackets = new LimitedQueue<Packet>(10);
//        onReceived = OnReceived;
//    }

//    public void Connect(IPAddress serverIp, int serverPort, int clientPort)
//    {
//        if (mNetState == NetState.None)
//        {
//            UConnect(serverIp, serverPort, clientPort);
//            Send(null, PacketType.Connect);
//            mNetState = NetState.Connecting;
//            lastConnectTime = DateTime.Now;
//            connectTimes++;
//        }
//    }

//    public void Send(byte[] data, PacketType type = PacketType.Message)
//    {
//        Packet packet = new Packet();
//        packet.type = type;
//        packet.id = id;
//        packet.sequence = ++sequence;
//        packet.ack = ack;
//        packet.ackbit = ackbit;
//        packet.data = data;
//        packet.sendTime = DateTime.Now;
//        quantics.Add(packet.sequence.value, packet);
//        byte[] bytes = packet.ToBytes();
//        if (bytes != null && bytes.Length > 0)
//        {
//            USend(bytes);
//        }
//    }

//    public void Close()
//    {
//        UClose();
//        sequence = 0;
//        ack = 0;
//        ackbit = 0;
//        quantics.Clear();
//        mLatency = NetConfig.RTT_GOOD;
//        mNetState = NetState.None;
//        mNetStates = new LimitedQueue<NetState>(10);
//        lastPacketReceviedTime = DateTime.Now;
//        lastConnectTime = DateTime.Now;
//        connectTimes = 0;
//        mLastPackets = new LimitedQueue<Packet>(10);
//    }

//    public LimitedQueue<Packet> lastPackets
//    {
//        get
//        {
//            return mLastPackets;
//        }
//    }

//    public float latency
//    {
//        get
//        {
//            return mLatency;
//        }
//    }

//    public NetState netState
//    {
//        get
//        {
//            return mNetState;
//        }
//    }

//    public bool connected
//    {
//        get
//        {
//            return mNetState > NetState.Connecting;
//        }
//    }

//    private void OnReceived(byte[] bytes)
//    {
//        Packet packet = new Packet();
//        if (packet.Parse(bytes))
//        {
//            if (packet.id == id)
//            {
//                OnAck(packet);
//            }
//            else
//            {
//                Debug.LogError("Receive Cross Packet: " + packet.id);
//            }
//        }
//    }

//    private void OnAck(Packet packet)
//    {
//        List<ushort> expired = new List<ushort>();
//        // 遍历处理超过32次收包未被应答的包
//        foreach (ushort seq in quantics.Keys)
//        {
//            if ((packet.ack - seq).value >= 32)
//            {
//                expired.Add(seq);
//                OnPacketLoss(quantics[seq]);
//            }
//        }
//        // 对已发包列表应用应答，移除已被应答的包
//        for (int i = 0; i < 32; i++)
//        {
//            if ((packet.ackbit & (1 << i)) > 0)
//            {
//                ushort seq = (packet.ack - i).value;
//                if (quantics.ContainsKey(seq))
//                {
//                    expired.Add(seq);
//                    OnPacketArrived(quantics[seq]);
//                }
//            }
//        }
//        foreach (ushort seq in expired)
//        {
//            quantics.Remove(seq);
//        }
//        // 写收包应答
//        if (packet.sequence > ack)
//        {
//            ackbit = ackbit << (int)(packet.sequence - ack).value;
//            ackbit |= (1 << 0);
//            ack = packet.sequence;
//            OnPacketReceived(packet);
//        }
//        else
//        {
//            uint value = (uint)(1 << (int)(ack - packet.sequence).value);
//            if ((ackbit & value) == 0)
//            {   
//                // 如果收到的包是未被处理的
//                ackbit |= value;
//                OnPacketReceived(packet);
//            }
//            else
//            {
//                Debug.Log("Receive packet already handle : sequence " + packet.sequence);
//            }
//        }
//    }

//    /// <summary>
//    /// 收包
//    /// </summary>
//    /// <param name="packet"></param>
//    protected virtual void OnPacketReceived(Packet packet)
//    {
//        if (mNetState == NetState.Connecting &&
//            packet.type == PacketType.Connect)
//        {
//            mNetState = NetState.Normal;
//        }
//        if (mNetState > NetState.Connecting)
//        {
//            mLastPackets.Enqueue(packet);
//            lastPacketReceviedTime = DateTime.Now;
//            if (packet.type == PacketType.HeartBeat)
//            {
//                Send(null, PacketType.HeartBeat);
//            }
//        }
//    }

//    /// <summary>
//    /// 确认发包已到达
//    /// </summary>
//    protected virtual void OnPacketArrived(Packet packet)
//    {
//        /*
//        if (connected &&
//            packet.type == PacketType.HeartBeat)
//        {
//            float lastHeartBeatLatency = Time.realtimeSinceStartup - packet.realTime;
//            mLatency = mLatency == 0 ? lastHeartBeatLatency : (mLatency * 0.9f + lastHeartBeatLatency * 0.1f);
//            NetState currentNetState = NetConfig.RTT_To_NetState(mLatency);
//            mNetStates.Enqueue(currentNetState);
//            if (mNetStates.Count == mNetStates.Limit)
//            {
//                float stv = 0;
//                Array.ForEach<NetState>(mNetStates.ToArray(), (NetState each) => stv += (int)each);
//                mNetState = (NetState)Mathf.RoundToInt(stv / mNetStates.Count);
//            }
//        }
//        */
//    }

//    /// <summary>
//    /// 确认发包超时未到达
//    /// </summary>
//    protected virtual void OnPacketLoss(Packet packet)
//    {
        
//    }

//    public void Update(int frame, float deltaTime)
//    {
//        if (mNetState == NetState.Connecting)
//        {
//            if ((DateTime.Now - lastConnectTime).TotalSeconds > NetConfig.ConnnectTimeout)
//            {
//                if (connectTimes < NetConfig.MaxConnectTimes)
//                {
//                    Send(null, PacketType.Connect);
//                    lastConnectTime = DateTime.Now;
//                    connectTimes++;
//                }
//                else
//                {
//                    Close();
//                }
//            }
//        }
//        else if (mNetState > NetState.Connecting)
//        {
//            if ((DateTime.Now - lastPacketReceviedTime).TotalSeconds > NetConfig.BreakTimeout)
//            {
//                Close();
//            }
//        }
//    }
//}
