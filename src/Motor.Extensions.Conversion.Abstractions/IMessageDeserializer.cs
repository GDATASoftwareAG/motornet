namespace Motor.Extensions.Conversion.Abstractions
{
    public interface IMessageDeserializer<out T>
    {
        T Deserialize(byte[] message);
    }
}
