using Jaeger.Thrift.Senders.Internal;

namespace Motor.Extensions.Diagnostics.Tracing.Config
{
    public class TracingConfig
    {
        public string AgentHost { get; set; } = "localhost";
        public int AgentPort { get; set; } = 6831;
        public double SamplingProbability { get; set; } = 0.001;
        
        /// <summary>
        /// If 0 it will use <see cref="ThriftUdpClientTransport.MaxPacketSize"/>.
        /// </summary>
        public int ReporterMaxPacketSize { get; set; } = 0;
    }
}
