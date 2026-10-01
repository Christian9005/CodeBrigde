using CodeBridge.Core.Exceptions;

namespace CodeBridge.Core.Tests.Exceptions;

public class ExceptionsTests
{
    [Fact]
    public void DeviceNotRespondingException_ShouldSetMessageAndInnerException()
    {
        var inner = new Exception("Inner test exception");
        var ex = new DeviceNotRespondingException("MyDevice", inner);
        
        Assert.Contains("MyDevice", ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }
    
    [Fact]
    public void DeviceNotRespondingException_DefaultConstructor_Works()
    {
        var ex = new DeviceNotRespondingException("MyDevice");
        Assert.Contains("MyDevice", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void ProtocolMismatchException_ShouldSetVersions()
    {
        var ex = new ProtocolMismatchException("1.0.0", "0.9.0");
        
        Assert.Equal("1.0.0", ex.ExpectedVersion);
        Assert.Equal("0.9.0", ex.ActualVersion);
        Assert.Contains("1.0.0", ex.Message);
        Assert.Contains("0.9.0", ex.Message);
    }
}
