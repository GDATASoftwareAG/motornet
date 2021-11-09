namespace Motor.Extensions.Conversion.Abstractions;

public interface IMessageSerializer<in T> where T : notnull
{
    byte[] Serialize(T message);
}
