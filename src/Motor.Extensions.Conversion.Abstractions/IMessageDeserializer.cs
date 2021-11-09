namespace Motor.Extensions.Conversion.Abstractions;

public interface IMessageDeserializer<out T> where T : notnull
{
    T Deserialize(byte[] message);
}
