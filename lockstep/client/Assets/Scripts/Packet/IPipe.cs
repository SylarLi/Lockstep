public interface IPipe
{
    bool Parse(byte[] bytes);

    byte[] ToBytes();
}
