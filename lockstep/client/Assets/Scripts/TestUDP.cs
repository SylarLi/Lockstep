using System.Collections.Generic;
using UnityEngine;
using System.Net;

/// <summary>
/// Unity physics engine is not deterministic!
/// </summary>
public class TestUDP : MonoBehaviour
{
    private uint ID = 1;
    private IPAddress serverAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
    private int serverPort = 9001;
    private int clientPort = 8001;

    private const float moveSpeed = 6;

    private float gspeed;

    private AppSocket socket;

    private LockStepState LSState;

    private float LSTick;

    private RUShortInt LSFrame;

    private bool LSFrameAchieve;

    private int LSAccFrame;

    private PlayersInputBuffer SLSBuffer;

    private GameObject[] players;

    private void Awake()
    {
        GameObject[] all = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in all)
        {
            if (go.rigidbody != null)
            {
                go.rigidbody.useConeFriction = false;
                go.rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                go.rigidbody.interpolation = RigidbodyInterpolation.None;
            }
        }
    }

    private void Start()
    {
        gspeed = 1;
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("ID");
        ID = uint.Parse(GUILayout.TextField(ID.ToString()));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("server ip");
        byte[] bip = serverAddress.GetAddressBytes();
        for (int i = 0; i < bip.Length; i++)
        {
            bip[i] = System.Convert.ToByte(GUILayout.TextField(bip[i].ToString()));
        }
        serverAddress = new IPAddress(bip);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("server port");
        serverPort = int.Parse(GUILayout.TextField(serverPort.ToString()));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("client port");
        clientPort = int.Parse(GUILayout.TextField(clientPort.ToString()));
        GUILayout.EndHorizontal();

        GUI.enabled = socket == null || socket.netState == NetState.None;
        if (GUILayout.Button("Connect", GUILayout.Width(200), GUILayout.Width(80)))
        {
            LSState = LockStepState.None;
            LSTick = 0;
            LSFrame = 0;
            LSFrameAchieve = true;
            LSAccFrame = 0;
            SLSBuffer = new PlayersInputBuffer();
            socket = new AppSocket(ID);
            socket.packetHandler[PacketType.Message] = ReceivePacketHandler;
            socket.packetHandler[PacketType.LockStep] = ReceivePacketHandler;
            socket.Connect(serverAddress, serverPort, clientPort);
        }
        GUI.enabled = true;

        GUI.color = socket != null && socket.netState == NetState.Connected ? Color.green : Color.white;
        GUILayout.Label("状态: " + (socket != null ? socket.netState : NetState.None));
        GUI.color = Color.white;
        GUILayout.Label("延迟: " + (socket != null ? ((int)(socket.latency * 1000)).ToString() : ""));
        GUILayout.Label("速度: " + gameSpeed);

        if (socket != null)
        {
            // 已发送包
            Packet[] sends = socket.sends.ToArray();
            for (int i = 0; i < sends.Length; i++)
            {
                GUI.color = sends[i].arrived ? Color.green : Color.white;
                GUILayout.Label(sends[i].sequence + " " + sends[i].type);
                GUI.color = Color.white;
            }
            // 已接收包
            GUILayout.BeginArea(new Rect(300, 20, 200, 600));
            Packet[] receives = socket.receives.ToArray();
            for (int i = 0; i < receives.Length; i++)
            {
                GUILayout.Label(receives[i].sequence + " " + receives[i].type);
            }
            GUILayout.EndArea();
        }
    }

    private void Update()
    {
        if (socket != null)
        {
            socket.Update(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (LSState > LockStepState.None)
        {
            if (LSFrameAchieve)
            {
                if (LSAccFrame == 0)
                {
                    float deltaTime = Time.fixedDeltaTime;
                    float accTime = LSTick + deltaTime;
                    LSAccFrame = Mathf.FloorToInt(accTime / NetConfig.TPF);
                    LSTick = accTime - LSAccFrame * NetConfig.TPF;
                }
                while (LSAccFrame > 0)
                {
                    SendPlayerInput();
                    LSAccFrame -= 1;
                    LSFrame += 1;
                    LSFrameAchieve = false;
                    if (LSState == LockStepState.Prepare)
                    {
                        if (LSFrame > new RUShortInt(NetConfig.PDBL - 1))
                        {
                            LSState = LockStepState.Start;
                        }
                        else
                        {
                            LSFrameAchieve = true;
                        }
                    }
                    if (LSState == LockStepState.Start)
                    {
                        LSFrameAchieve = LockStepUpdate();
                    }
                    if (!LSFrameAchieve) break;
                }
            }
            else
            {
                LSFrameAchieve = LockStepUpdate();
            }
        }
    }

    private bool LockStepUpdate()
    {
        float speedTo = 1;

        RUShortInt CFrame = LSFrame + RUShortInt.Reverse(NetConfig.PDBL);
        List<PlayersInput> buffer = SLSBuffer.buffer;
        int lsIndex = buffer.FindIndex(0, buffer.Count, (PlayersInput each) => each.frame == CFrame);
        if (lsIndex == -1)
        {
            speedTo = 0;
        }
        else
        {
            RUShortInt minFrame = buffer[0].frame;
            RUShortInt maxFrame = buffer[buffer.Count - 1].frame;
            if (CFrame == minFrame)
            {
                uint dist = (maxFrame - minFrame).value;
                speedTo = dist <= NetConfig.PDBL ? 1 : 2;
            }
            else
            {
                speedTo = 0;
            }
        }

        gameSpeed = speedTo;
        if (gameSpeed > 0)
        {
            ApplyPlayersInput(SLSBuffer.buffer[0]);
            SLSBuffer.buffer.RemoveAt(0);
            return true;
        }
        return false;
    }

    private void ApplyPlayersInput(PlayersInput input)
    {
        if (players == null)
        {
            players = new GameObject[input.datas.Count];
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = GameObject.Find("Cube" + i);
            }
        }
        List<PlayerInput> inputs = input.datas;
        for (int i = 0; i < inputs.Count; i++)
        {
            Vector3 delta = Vector3.zero;
            float speedDelta = NetConfig.TPF * moveSpeed;
            if (inputs[i].up)
            {
                delta.z += speedDelta;
            }
            if (inputs[i].right)
            {
                delta.x += speedDelta;
            }
            if (inputs[i].down)
            {
                delta.z -= speedDelta;
            }
            if (inputs[i].left)
            {
                delta.x -= speedDelta;
            }
            if (delta.x != 0 || delta.z != 0)
            {
                players[i].transform.localPosition += delta;
            }
            //Vector3 delta = Vector3.zero;
            //float forceDelta = 10;
            //if (inputs[i].up)
            //{
            //    delta.z += forceDelta;
            //}
            //if (inputs[i].right)
            //{
            //    delta.x += forceDelta;
            //}
            //if (inputs[i].down)
            //{
            //    delta.z -= forceDelta;
            //}
            //if (inputs[i].left)
            //{
            //    delta.x -= forceDelta;
            //}
            //if (delta.x != 0 || delta.z != 0)
            //{
            //    players[i].rigidbody.AddForce(delta);
            //}
        }
    }

    private void SendPlayerInput()
    {
        PlayerInput input = new PlayerInput(LSFrame);
        if (Input.GetKey(KeyCode.UpArrow)) input.up = true;
        if (Input.GetKey(KeyCode.DownArrow)) input.down = true;
        if (Input.GetKey(KeyCode.LeftArrow)) input.left = true;
        if (Input.GetKey(KeyCode.RightArrow)) input.right = true;
        byte[] LSData = input.ToBytes();
        socket.Send(LSData, PacketType.LockStep);
    }

    private void ReceivePacketHandler(Packet packet)
    {
        if (packet.type == PacketType.Message)
        {
            if (LSState == LockStepState.None)
            {
                LSState = LockStepState.Prepare;
            }
        }
        else if (packet.type == PacketType.LockStep)
        {
            PlayersInputBuffer pib = new PlayersInputBuffer();
            pib.Parse(packet.data);
            RUShortInt CFrame = LSFrame + RUShortInt.Reverse(NetConfig.PDBL);
            int index = pib.buffer.FindIndex((PlayersInput each) => each.frame == CFrame);
            if (index > 0)
            {
                pib.buffer.RemoveRange(0, index);
            }
            SLSBuffer.Merge(pib);
        }
    }

    public float gameSpeed
    {
        get
        {
            return gspeed;
        }
        set
        {
            if (gspeed != value)
            {
                gspeed = value;
                OnSpeed();
            }
        }
    }

    private void OnSpeed()
    {
        Time.fixedDeltaTime *= gameSpeed;
    }

    private void OnDestroy()
    {
        if (socket != null)
        {
            socket.Close();
        }
    }
}
