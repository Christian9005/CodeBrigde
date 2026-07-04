using CodeBridge.Flow;
using CodeBridge.Flow.Validation;

namespace CodeBridge.Core.Tests.Flow;

public class BuiltInFlowPresetsTests
{
    [Theory]
    [InlineData(2, 500, false)]
    [InlineData(4, 1000, true)]
    public void CreateBlinkOnce_ReturnsValidFlow(int pin, int delayMs, bool activeLow)
    {
        var document = BuiltInFlowPresets.CreateBlinkOnce(pin, delayMs, activeLow);

        var result = new FlowValidator().Validate(document, BuiltInBlockCatalog.Create());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(document.Nodes, node =>
            node.Type == BuiltInBlockCatalog.GpioBlinkLed &&
            Convert.ToInt32(node.Parameters["pin"]) == pin &&
            Convert.ToInt32(node.Parameters["durationMs"]) == delayMs);
    }

    [Fact]
    public void CreateDigitalReadDebug_ReturnsValidFlow()
    {
        var document = BuiltInFlowPresets.CreateDigitalReadDebug(4);

        var result = new FlowValidator().Validate(document, BuiltInBlockCatalog.Create());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(document.Nodes, node =>
            node.Type == BuiltInBlockCatalog.GpioDigitalRead &&
            Convert.ToInt32(node.Parameters["pin"]) == 4);
    }

    [Fact]
    public void CreateAnalogStreamDashboard_ReturnsValidFlow()
    {
        var document = BuiltInFlowPresets.CreateAnalogStreamDashboard(32, sampleRateHz: 1000);

        var result = new FlowValidator().Validate(document, BuiltInBlockCatalog.Create());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(document.Nodes, node =>
            node.Type == BuiltInBlockCatalog.SampleChannel &&
            Convert.ToInt32(node.Parameters["pin"]) == 32);
        Assert.Contains(document.Nodes, node => node.Type == BuiltInBlockCatalog.StreamDashboard);
    }
}
