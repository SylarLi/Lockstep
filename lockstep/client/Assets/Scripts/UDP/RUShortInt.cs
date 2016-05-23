/// <summary>
/// UDP Round Sequence 专用
/// </summary>
public struct RUShortInt
{
    public const byte MAX_VALUE_2 = byte.MaxValue / 2;

    public byte value;

    public RUShortInt(short value)
    {
        this.value = (byte)value;
    }

    public RUShortInt(byte value)
    {
        this.value = value;
    }

    public RUShortInt(int value)
    {
        this.value = (byte)value;
    }

    public RUShortInt(uint value)
    {
        this.value = (byte)value;
    }

    public static implicit operator RUShortInt(byte val)
    {
        return new RUShortInt(val);
    }

    public static implicit operator RUShortInt(short val)
    {
        return new RUShortInt(val);
    }

    public static implicit operator RUShortInt(uint val)
    {
        return new RUShortInt(val);
    }

    public static implicit operator RUShortInt(int val)
    {
        return new RUShortInt(val);
    }

    /// <summary>
    /// 循环比较大小
    /// </summary>
    /// <param name="val1"></param>
    /// <param name="val2"></param>
    /// <returns></returns>
    public static bool operator >(RUShortInt val1, RUShortInt val2)
    {
        return (val1.value > val2.value && (val1 - val2).value <= MAX_VALUE_2) ||
            (val1.value < val2.value && (val2 - val1).value >= MAX_VALUE_2);
    }

    /// <summary>
    /// 循环比较大小
    /// </summary>
    /// <param name="val1"></param>
    /// <param name="val2"></param>
    /// <returns></returns>
    public static bool operator <(RUShortInt val1, RUShortInt val2)
    {
        return val2 > val1;
    }

    public static bool operator ==(RUShortInt val1, RUShortInt val2)
    {
        return val1.value == val2.value;
    }

    public static bool operator !=(RUShortInt val1, RUShortInt val2)
    {
        return !(val1 == val2);
    }

    public static RUShortInt operator +(RUShortInt val1, RUShortInt val2)
    {
        int value = val1.value + val2.value;
        return new RUShortInt(value > byte.MaxValue ? (value - byte.MaxValue - 1) : value);
    }

    /// <summary>
    /// 距离值，始终为正值
    /// </summary>
    /// <param name="val1"></param>
    /// <param name="val2"></param>
    /// <returns></returns>
    public static RUShortInt operator -(RUShortInt val1, RUShortInt val2)
    {
        return new RUShortInt(val1.value >= val2.value ? (val1.value - val2.value) : (val1.value + byte.MaxValue + 1 - val2.value));
    }

    public static RUShortInt operator ++(RUShortInt val)
    {
        return new RUShortInt(val.value == byte.MaxValue ? byte.MinValue : ++val.value);
    }

    public override string ToString()
    {
        return value.ToString();
    }

    /// <summary>
    /// 取反
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public static RUShortInt Reverse(RUShortInt val)
    {
        return new RUShortInt(byte.MaxValue - val.value + 1);
    }
}
