using System.Runtime.Serialization;

namespace VBY.PluginLoader.SingleFileExtractor.Core.Exceptions;

public class UnsupportedExecutableException : Exception
{
    public UnsupportedExecutableException()
    {
    }

    public UnsupportedExecutableException(string message)
        : base(message)
    {
    }

    public UnsupportedExecutableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
    
    [Obsolete("Obsolete")]
    protected UnsupportedExecutableException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}