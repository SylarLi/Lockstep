using System.IO;

public class PlayerInput : Pipe, ILSData
{
    // 帧序号
    private RUShortInt mFrame;

    // bit 0
    public bool up;

    // bit 1
    public bool right;

    // bit 2
    public bool down;

    // bit 3
    public bool left;

    public PlayerInput(RUShortInt frame) : base()
    {
        mFrame = frame;
    }

    public RUShortInt frame
    {
        get
        {
            return mFrame;
        }
    }

    public override void Parse(BinaryReader reader)
    {
        byte dirs = reader.ReadByte();
        up = (dirs & 1) > 0;
        right = (dirs & (1 << 1)) > 0;
        down = (dirs & (1 << 2)) > 0;
        left = (dirs & (1 << 3)) > 0;
    }

    public override void ToBytes(BinaryWriter writer)
    {
        byte dirs = 0;
        if (up) dirs |= 1;
        if (right) dirs |= (1 << 1);
        if (down) dirs |= (1 << 2);
        if (left) dirs |= (1 << 3);
        writer.Write(mFrame.value);
        writer.Write(dirs);
    }
}
