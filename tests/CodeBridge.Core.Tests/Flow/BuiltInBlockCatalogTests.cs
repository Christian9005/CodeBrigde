using CodeBridge.Flow;

namespace CodeBridge.Core.Tests.Flow;

public class BuiltInBlockCatalogTests
{
    [Fact]
    public void Esp32DevKit_ExposesFormalPinCapabilities()
    {
        var servoPin = BuiltInBoardProfiles.Esp32DevKit.FindPin(13);
        var inputOnlyPin = BuiltInBoardProfiles.Esp32DevKit.FindPin(34);

        Assert.NotNull(servoPin);
        Assert.True(servoPin.HasCapability(PinCapability.ServoRecommended));
        Assert.True(servoPin.HasCapability(PinCapability.Pwm));
        Assert.True(servoPin.HasCapability(PinCapability.Adc2));
        Assert.False(servoPin.HasCapability(PinCapability.BootStrap));

        Assert.NotNull(inputOnlyPin);
        Assert.True(inputOnlyPin.HasCapability(PinCapability.InputOnly));
        Assert.True(inputOnlyPin.HasCapability(PinCapability.Adc1));
        Assert.False(inputOnlyPin.HasCapability(PinCapability.DigitalWrite));
    }

    [Fact]
    public void ArduinoUno_ExposesBoardSpecificPinCapabilities()
    {
        var board = BuiltInBoardProfiles.ArduinoUno;

        Assert.Contains(BuiltInBoardProfiles.All, profile => profile.Id == "arduino-uno");
        Assert.Contains(board.DigitalWritePinOptions, option =>
            Equals(option.Value, 13) &&
            option.Label.Contains("D13", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(board.DigitalWritePinOptions, option => Equals(option.Value, 0));
        Assert.DoesNotContain(board.DigitalWritePinOptions, option => Equals(option.Value, 1));

        Assert.Contains(board.AnalogReadPinOptions, option =>
            Equals(option.Value, 14) &&
            option.Label.Contains("A0", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(board.AnalogReadPinOptions, option => Equals(option.Value, 13));

        Assert.Contains(board.PwmPinOptions, option => Equals(option.Value, 3));
        Assert.Contains(board.PwmPinOptions, option => Equals(option.Value, 11));
        Assert.DoesNotContain(board.PwmPinOptions, option => Equals(option.Value, 13));

        Assert.Contains(board.ServoPinOptions, option => Equals(option.Value, 9));
        Assert.Contains(board.ServoPinOptions, option => Equals(option.Value, 10));
        Assert.DoesNotContain(board.ServoPinOptions, option => Equals(option.Value, 3));

        Assert.Contains(board.InterruptPinOptions, option => Equals(option.Value, 2));
        Assert.Contains(board.InterruptPinOptions, option => Equals(option.Value, 3));
    }

    [Fact]
    public void Create_UsesRequestedBoardProfileForPinOptions()
    {
        var board = new BoardProfile(
            "test-board",
            "Test Board",
            [
                new(1, "Input", "Digital input only.", PinCapability.DigitalRead),
                new(2, "Output", "Digital output only.", PinCapability.DigitalWrite)
            ]);

        var catalog = BuiltInBlockCatalog.Create(board);

        var writePin = catalog
            .Get(BuiltInBlockCatalog.GpioDigitalWrite)
            .Properties
            .Single(property => property.Name == "pin");

        Assert.Contains(writePin.Options!, option => Equals(option.Value, 2));
        Assert.DoesNotContain(writePin.Options!, option => Equals(option.Value, 1));
    }

    [Fact]
    public void GpioBlocks_ExposeEsp32PinOptions()
    {
        var catalog = BuiltInBlockCatalog.Create();

        var writePin = catalog
            .Get(BuiltInBlockCatalog.GpioDigitalWrite)
            .Properties
            .Single(property => property.Name == "pin");

        Assert.Contains(writePin.Options!, option =>
            Equals(option.Value, 2) &&
            option.Label.Contains("GPIO 2", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(writePin.Options!, option => Equals(option.Value, 34));

        var analogPin = catalog
            .Get(BuiltInBlockCatalog.GpioAnalogRead)
            .Properties
            .Single(property => property.Name == "pin");

        Assert.Contains(analogPin.Options!, option => Equals(option.Value, 34));
    }

    [Fact]
    public void PinModeBlock_ExposesModeOptions()
    {
        var catalog = BuiltInBlockCatalog.Create();

        var mode = catalog
            .Get(BuiltInBlockCatalog.GpioPinMode)
            .Properties
            .Single(property => property.Name == "mode");

        Assert.Contains(mode.Options!, option => Equals(option.Value, "Output"));
        Assert.Contains(mode.Options!, option => Equals(option.Value, "InputPullUp"));
    }

    [Fact]
    public void DebugBlock_AcceptsAnyValueInput()
    {
        var catalog = BuiltInBlockCatalog.Create();

        var debug = catalog.Get(BuiltInBlockCatalog.DebugLog);

        Assert.Equal("Debug", debug.Category);
        Assert.Contains(debug.InputPorts, port =>
            port.Name == "value" &&
            port.ValueKind == FlowValueKind.Any &&
            !port.Required);
        Assert.Contains(debug.OutputPorts, port =>
            port.Name == "message" &&
            port.ValueKind == FlowValueKind.String);
    }

    [Fact]
    public void AcquisitionBlocks_ExposeSamplingAndDashboardOptions()
    {
        var catalog = BuiltInBlockCatalog.Create();

        var sample = catalog.Get(BuiltInBlockCatalog.SampleChannel);
        Assert.Equal("Acquisition", sample.Category);
        Assert.Contains(sample.OutputPorts, port => port.Name == "samples" && port.ValueKind == FlowValueKind.Any);
        Assert.Contains(sample.Properties, property => property.Name == "mode" && property.Options!.Any(option => Equals(option.Value, "HardwareTimer")));
        Assert.Contains(sample.Properties, property => property.Name == "backpressure" && property.Options!.Any(option => Equals(option.Value, "DropOldest")));

        var interrupt = catalog.Get(BuiltInBlockCatalog.InterruptInput);
        Assert.Equal("Acquisition", interrupt.Category);
        Assert.Contains(interrupt.Properties.Single(property => property.Name == "pin").Options!, option => Equals(option.Value, 34));

        var dashboard = catalog.Get(BuiltInBlockCatalog.StreamDashboard);
        Assert.Equal("Dashboard", dashboard.Category);
        Assert.Contains(dashboard.Properties, property => property.Name == "maxPoints");
    }

    [Fact]
    public void ServoBlock_ExposesOnlyRecommendedServoPins()
    {
        var catalog = BuiltInBlockCatalog.Create();

        var servoPin = catalog
            .Get(BuiltInBlockCatalog.ServoWrite)
            .Properties
            .Single(property => property.Name == "pin");

        Assert.Contains(servoPin.Options!, option => Equals(option.Value, 13));
        Assert.DoesNotContain(servoPin.Options!, option => Equals(option.Value, 4));
        Assert.DoesNotContain(servoPin.Options!, option => Equals(option.Value, 34));
        Assert.Equal(13, servoPin.DefaultValue);
    }
}
