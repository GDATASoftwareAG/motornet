// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: OutputMsg.proto

#pragma warning disable 1591, 0612, 3021

#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;

namespace Motor.Extensions.Conversion.Protobuf_UnitTest
{
    /// <summary>Holder for reflection information generated from OutputMsg.proto</summary>
    public static partial class OutputMsgReflection
    {
        #region Descriptor

        /// <summary>File descriptor for OutputMsg.proto</summary>
        public static pbr::FileDescriptor Descriptor
        {
            get { return descriptor; }
        }

        private static pbr::FileDescriptor descriptor;

        static OutputMsgReflection()
        {
            byte[] descriptorData = global::System.Convert.FromBase64String(
                string.Concat(
                    "Cg9PdXRwdXRNc2cucHJvdG8SHmp1bXBzdGFydC5tZXNzYWdlcy5wcm90bzNf",
                    "dGVzdCIhCg1PdXRwdXRNZXNzYWdlEhAKCGdyZWV0aW5nGAEgASgJQiGqAh5K",
                    "dW1wc3RhcnQuTWVzc2FnZXMuUHJvdG8zX1Rlc3RiBnByb3RvMw=="));
            descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
                new pbr::FileDescriptor[] { },
                new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[]
                {
                    new pbr::GeneratedClrTypeInfo(
                        typeof(global::Motor.Extensions.Conversion.Protobuf_UnitTest.OutputMessage),
                        global::Motor.Extensions.Conversion.Protobuf_UnitTest.OutputMessage.Parser, new[] {"Greeting"},
                        null, null, null)
                }));
        }

        #endregion
    }

    #region Messages

    public sealed partial class OutputMessage : pb::IMessage<OutputMessage>
    {
        /// <summary>Field number for the "greeting" field.</summary>
        public const int GreetingFieldNumber = 1;

        private static readonly pb::MessageParser<OutputMessage> _parser =
            new pb::MessageParser<OutputMessage>(() => new OutputMessage());

        private string greeting_ = "";

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OutputMessage()
        {
            OnConstruction();
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OutputMessage(OutputMessage other) : this()
        {
            greeting_ = other.greeting_;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pb::MessageParser<OutputMessage> Parser
        {
            get { return _parser; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pbr::MessageDescriptor Descriptor
        {
            get
            {
                return global::Motor.Extensions.Conversion.Protobuf_UnitTest.OutputMsgReflection.Descriptor
                    .MessageTypes[0];
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string Greeting
        {
            get { return greeting_; }
            set { greeting_ = pb::ProtoPreconditions.CheckNotNull(value, "value"); }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        pbr::MessageDescriptor pb::IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OutputMessage Clone()
        {
            return new OutputMessage(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(OutputMessage other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (Greeting != other.Greeting) return false;
            return true;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output)
        {
            if (Greeting.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteString(Greeting);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            if (Greeting.Length != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeStringSize(Greeting);
            }

            return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(OutputMessage other)
        {
            if (other == null)
            {
                return;
            }

            if (other.Greeting.Length != 0)
            {
                Greeting = other.Greeting;
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        input.SkipLastField();
                        break;
                    case 10:
                    {
                        Greeting = input.ReadString();
                        break;
                    }
                }
            }
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as OutputMessage);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            if (Greeting.Length != 0) hash ^= Greeting.GetHashCode();
            return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return pb::JsonFormatter.ToDiagnosticString(this);
        }
    }

    #endregion
}

#endregion Designer generated code