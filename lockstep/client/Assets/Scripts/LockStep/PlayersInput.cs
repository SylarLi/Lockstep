using System.Collections.Generic;
using System.IO;

public class PlayersInput : Pipe, ILSDataList<PlayerInput>
{
    private RUShortInt mFrame;

    private List<PlayerInput> mDatas;

    public PlayersInput() : base()
    {
        mDatas = new List<PlayerInput>();
    }

    public RUShortInt frame
    {
        get
        {
            return mFrame;
        }
    }

    public List<PlayerInput> datas
    {
        get
        {
            return mDatas;
        }
    }

    public override void Parse(BinaryReader reader)
    {
        mFrame = reader.ReadByte();
        mDatas.Clear();
        byte length = reader.ReadByte();
        for (byte i = 0; i < length; i++)
        {
            PlayerInput input = new PlayerInput(mFrame);
            input.Parse(reader);
            mDatas.Add(input);
        }
    }
}
