using CodeBridge.Core;
using CodeBridge.ESP32;
using CodeBridge.Transport;

namespace CodeBridge.Samples.IntegrationWindowsService;

/// <summary>Reads GPIO 34 once a minute and appends it to %ProgramData%\CodeBridge\readings.csv. Reconnects if the board disappears.</summary>
public sealed class SensorLoggerWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private readonly ILogger<SensorLoggerWorker> _logger;
    private readonly string _csvPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CodeBridge", "readings.csv");

    public SensorLoggerWorker(ILogger<SensorLoggerWorker> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_csvPath)!);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var port = BoardDiscovery.DiscoverPorts().FirstOrDefault()?.Name
                           ?? throw new InvalidOperationException("No board found.");

                await using var board = await CodeBridgeBuilder.Connect().Serial(port).ToESP32().BuildAsync(stoppingToken);
                _logger.LogInformation("Connected to {Port}, firmware {Firmware}", port, board.FirmwareVersion);

                while (!stoppingToken.IsCancellationRequested && board.IsConnected)
                {
                    var raw = await board.Gpio.AnalogReadAsync(34, stoppingToken);
                    await File.AppendAllTextAsync(_csvPath, $"{DateTimeOffset.Now:O},{raw}{Environment.NewLine}", stoppingToken);
                    await Task.Delay(Interval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Board unplugged or not ready: wait and try again instead of crashing the service.
                _logger.LogWarning("Board unavailable ({Message}). Retrying in 15 s.", ex.Message);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
