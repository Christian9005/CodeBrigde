using CodeBridge.Core;
using CodeBridge.Core.Enums;
using CodeBridge.ESP32;
using CodeBridge.Flow;
using CodeBridge.Flow.Execution;
using CodeBridge.Flow.Serialization;
using CodeBridge.Transport.Simulation;

namespace CodeBridge.Core.Tests.Transport;

/// <summary>The virtual board must be indistinguishable from the firmware for everything the SDK sends.</summary>
public class SimulatedTransportTests
{
    private static async Task<(ESP32Board Board, SimulatedTransport Sim)> ConnectAsync(SimulatedBoardOptions? options = null)
    {
        var sim = new SimulatedTransport(options);
        var board = new ESP32Board(sim);
        await board.ConnectAsync();
        return (board, sim);
    }

    [Fact]
    public async Task The_builder_connects_to_the_simulator_without_hardware()
    {
        await using var board = await CodeBridgeBuilder.Connect().Simulator().ToESP32().BuildAsync();

        var info = await board.GetInfoAsync();

        Assert.True(board.IsConnected);
        Assert.Equal(BridgeProtocolVersion, board.FirmwareVersion);
        Assert.Equal("ESP32-SIM", info.ChipModel);
    }

    private static string BridgeProtocolVersion => CodeBridge.Core.Protocol.BridgeProtocol.EXPECTED_FIRMWARE_VERSION;

    [Fact]
    public async Task Digital_outputs_and_inputs_behave_like_pins()
    {
        var (board, sim) = await ConnectAsync();
        var changes = new List<SimulatedBoardEvent>();
        sim.BoardChanged += changes.Add;

        await board.Gpio.SetPinModeAsync(2, PinMode.Output);
        await board.Gpio.DigitalWriteAsync(2, PinValue.High);
        Assert.True(sim.GetOutput(2));
        Assert.Equal(PinValue.High, await board.Gpio.DigitalReadAsync(2));
        Assert.Contains(changes, c => c is { Kind: "digital", Pin: 2, Value: 1 });

        await board.Gpio.SetPinModeAsync(4, PinMode.Input);
        Assert.Equal(PinValue.Low, await board.Gpio.DigitalReadAsync(4));
        sim.SetDigitalInput(4, true); // "press the button"
        Assert.Equal(PinValue.High, await board.Gpio.DigitalReadAsync(4));
    }

    [Fact]
    public async Task Analog_inputs_follow_a_wave_inside_the_ADC_range_or_a_pinned_value()
    {
        var (board, sim) = await ConnectAsync();

        for (var i = 0; i < 20; i++)
        {
            var value = await board.Gpio.AnalogReadAsync(34);
            Assert.InRange(value, 0, 4095);
        }

        sim.SetAnalogInput(34, 1234);
        Assert.Equal(1234, await board.Gpio.AnalogReadAsync(34));

        sim.ReleaseAnalogInput(34);
        Assert.InRange(await board.Gpio.AnalogReadAsync(34), 0, 4095);
    }

    [Fact]
    public async Task An_Arduino_sized_simulator_keeps_analog_reads_within_ten_bits()
    {
        var (board, _) = await ConnectAsync(new SimulatedBoardOptions { AnalogMax = 1023, MaxPin = 19, Seed = 7 });

        for (var i = 0; i < 20; i++)
            Assert.InRange(await board.Gpio.AnalogReadAsync(14), 0, 1023);
    }

    [Fact]
    public async Task Pwm_servo_and_sensors_work_and_can_be_scripted()
    {
        var (board, sim) = await ConnectAsync();
        sim.Temperature = 31.0;
        sim.Distance = 12.0;

        await board.Gpio.PwmWriteAsync(2, 200);
        Assert.Equal(200, sim.GetPwmDuty(2));

        var servo = board.CreateServo(13);
        await servo.InitAsync();
        await servo.SetAngleAsync(120);
        Assert.Equal(120, sim.GetServoAngle(13));

        var dht = board.CreateDhtSensor(4);
        await dht.InitAsync();
        var temperature = await dht.ReadAsync();
        Assert.InRange(temperature, 30.0, 32.0);

        var ultrasonic = board.CreateUltrasonicSensor(5, 18);
        Assert.InRange(await ultrasonic.ReadAsync(), 11.0, 13.0);
    }

    [Fact]
    public async Task Invalid_pins_and_unknown_commands_are_rejected_like_the_firmware_does()
    {
        var (board, _) = await ConnectAsync();

        Assert.Equal("ERR:Invalid pin", await board.Transport.SendCommandAsync("DW:99:1\n"));

        var unknown = await board.Transport.SendCommandAsync("NOPE:1\n");
        Assert.StartsWith("ERR", unknown);
    }

    [Fact]
    public async Task Buffered_sampling_returns_ordered_frames_and_stops()
    {
        var (board, _) = await ConnectAsync();
        var channel = await board.Acquisition.StartSamplingAsync(new CodeBridge.Core.Abstractions.Acquisition.BoardSampleChannelRequest(
            Pin: 34, IsAnalog: true, Mode: CodeBridge.Core.Abstractions.Acquisition.BoardSamplingMode.Polling, SampleRateHz: 1000, BufferCapacity: 64, Backpressure: CodeBridge.Core.Abstractions.Acquisition.BoardBackpressurePolicy.DropOldest, BatchSize: 16));

        await Task.Delay(120);
        var frames = await board.Acquisition.ReadSamplesAsync(channel.Id, 16);

        Assert.NotEmpty(frames);
        Assert.True(frames.Count <= 16);
        Assert.Equal(frames.Select(f => f.Sequence).OrderBy(s => s), frames.Select(f => f.Sequence));

        await board.Acquisition.StopSamplingAsync(channel.Id);
    }

    [Fact]
    public async Task The_command_log_is_bounded()
    {
        var (_, sim) = await ConnectAsync();

        for (var i = 0; i < 1500; i++)
            await sim.SendCommandAsync("PING\n");

        Assert.Equal(1000, sim.CommandLog.Count);
    }

    [Fact]
    public async Task A_shipped_example_flow_runs_on_the_simulator()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "CodeBridge.VisualStudio", "ItemTemplates")))
            directory = directory.Parent;

        var path = Directory.EnumerateFiles(Path.Combine(directory!.FullName, "src", "CodeBridge.VisualStudio", "ItemTemplates"), "Example3_NightLight.cbflow", SearchOption.AllDirectories).Single();
        var document = FlowDocumentJson.Deserialize(File.ReadAllText(path));
        var (board, sim) = await ConnectAsync();
        sim.SetAnalogInput(34, 100); // a dark room

        var result = await new FlowRuntime(BuiltInBlockCatalog.Create(BuiltInBoardProfiles.Esp32DevKit)).ExecuteAsync(document, board);

        Assert.True(result.GetOutput<bool>("dark", "result")); // 100 is below the 2000 threshold
    }
}
