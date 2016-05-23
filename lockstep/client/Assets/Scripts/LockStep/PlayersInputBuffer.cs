using System.Collections.Generic;
using System.IO;

public class PlayersInputBuffer : Pipe, ILSDataBuffer<PlayersInput, PlayerInput>
{
    private List<PlayersInput> mBuffer;

    public PlayersInputBuffer() : base()
    {
        mBuffer = new List<PlayersInput>();
    }

    public List<PlayersInput> buffer
    {
        get
        {
            return mBuffer;
        }
    }

    public void Merge(ILSDataBuffer<PlayersInput, PlayerInput> pib)
    {
        if (pib != null && pib.buffer.Count > 0)
        {
            foreach (PlayersInput data in pib.buffer)
            {
                if (!mBuffer.Exists((PlayersInput each) => each.frame == data.frame))
                {
                    mBuffer.Add(data);
                }
            }
            mBuffer.Sort(SortByFrame);
        }
    }

    private int SortByFrame(PlayersInput data1, PlayersInput data2)
    {
        if (data1 == data2 || data1.frame == data2.frame) return 0;
        return data1.frame > data2.frame ? 1 : -1;
    }

    public override void Parse(BinaryReader reader)
    {
        mBuffer.Clear();
        uint length = reader.ReadUInt16();
        for (uint i = 0; i < length; i++)
        {
            PlayersInput input = new PlayersInput();
            input.Parse(reader);
            mBuffer.Add(input);
        }
    }
}
