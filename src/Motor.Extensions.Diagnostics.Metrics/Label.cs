namespace Motor.Extensions.Diagnostics.Metrics
{
    public abstract class AbstractLabel
    {
    }

    public class Label<T> : AbstractLabel
    {
        public T? Value { get; set; }

        public Label(T? value)
        {
            Value = value;
        }

        public override string? ToString()
        {
            return Value?.ToString();
        }
    }
}
