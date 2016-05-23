using System.Collections.Generic;

public class LimitedQueue<T> : Queue<T>
{
    private uint mLimit;

    public LimitedQueue(uint limit)
    {
        mLimit = limit;
    }

    public uint Limit
    {
        get
        {
            return mLimit;
        }
    }

    public new void Enqueue(T item)
    {
        if (Count >= Limit)
        {
            Dequeue();
        }
        base.Enqueue(item);
    }
}
