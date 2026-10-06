using CodeBridge.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodeBridge.Core.Tests.Hosting;

public class BoardServiceTests
{
    private static ServiceProvider Provider(Action<CodeBridgeOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCodeBridge(o =>
        {
            o.Port = "simulator";
            o.AutoConnect = false;
            configure?.Invoke(o);
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task The_first_command_connects_to_the_simulator()
    {
        await using var provider = Provider();
        var board = provider.GetRequiredService<BoardService>();
        Assert.Equal(BoardConnectionState.Disconnected, board.State);

        var value = await board.ReadAnalogAsync(34);

        Assert.InRange(value, 0, 4095);
        Assert.Equal(BoardConnectionState.Connected, board.State);
        Assert.True(board.IsSimulated);
        Assert.Equal("simulator", board.Target);
        Assert.NotNull(board.FirmwareVersion);
    }

    [Fact]
    public async Task Writes_reach_the_board_and_the_state_change_is_announced()
    {
        await using var provider = Provider();
        var board = provider.GetRequiredService<BoardService>();
        var changes = 0;
        board.Changed += () => Interlocked.Increment(ref changes);

        await board.WriteDigitalAsync(2, true);
        await board.WritePwmAsync(4, 300); // clamped to 255

        Assert.True(board.Simulator!.GetOutput(2));
        Assert.Equal(255, board.Simulator.GetPwmDuty(4));
        Assert.True(changes >= 2); // Connecting + Connected
    }

    [Fact]
    public async Task Many_callers_share_one_connection_safely()
    {
        await using var provider = Provider();
        var board = provider.GetRequiredService<BoardService>();

        var tasks = Enumerable.Range(0, 200).Select(i => i % 2 == 0
            ? board.WriteDigitalAsync(2, i % 4 == 0)
            : (Task)board.ReadAnalogAsync(34));
        await Task.WhenAll(tasks);

        Assert.Equal(BoardConnectionState.Connected, board.State);
    }

    [Fact]
    public async Task An_Arduino_board_uses_the_ten_bit_range()
    {
        await using var provider = Provider(o => o.Board = BoardKind.ArduinoUno);
        var board = provider.GetRequiredService<BoardService>();

        Assert.Equal(1023, board.AnalogMax);
        for (var i = 0; i < 20; i++)
            Assert.InRange(await board.ReadAnalogAsync(14), 0, 1023);
    }

    [Fact]
    public async Task A_missing_board_faults_without_hanging_and_disposes_cleanly()
    {
        await using var provider = Provider(o =>
        {
            o.Port = "COM255";
            o.ReconnectDelay = TimeSpan.FromMilliseconds(20);
            o.MaxReconnectDelay = TimeSpan.FromMilliseconds(50);
        });
        var board = provider.GetRequiredService<BoardService>();

        await Assert.ThrowsAnyAsync<Exception>(() => board.ConnectAsync());

        Assert.Equal(BoardConnectionState.Faulted, board.State);
        Assert.False(string.IsNullOrWhiteSpace(board.LastError));
        await Task.Delay(150); // lets the background retries run at least once
    }

    [Fact]
    public async Task Options_can_come_from_configuration_and_the_hosted_service_connects_at_startup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CodeBridge:Port"] = "simulator", ["CodeBridge:Board"] = "ArduinoUno" })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCodeBridge(configuration);
        await using var provider = services.BuildServiceProvider();

        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);

        var board = provider.GetRequiredService<BoardService>();
        for (var i = 0; i < 50 && !board.IsConnected; i++)
            await Task.Delay(50);

        Assert.True(board.IsConnected);
        Assert.Equal(1023, board.AnalogMax);
    }
}
