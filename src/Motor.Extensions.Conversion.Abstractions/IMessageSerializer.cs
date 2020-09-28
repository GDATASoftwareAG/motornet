namespace Motor.Extensions.Conversion.Abstractions
{
    public interface IMessageSerializer<in T>
    {
        byte[] Serialize(T message);
    }
}