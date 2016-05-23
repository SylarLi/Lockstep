using Core;

public class SocketEvent : Event
{
    public const string StateChange = "StateChange";

    public SocketEvent(string type, object data = null) : base(type, data)
    {

    }
}
