// CodeBridge behind a web API: control an LED and read a sensor over HTTP.
//   dotnet run         then:   curl -X POST http://localhost:5000/led/on
//                              curl http://localhost:5000/sensor/34
// Set the board's port in appsettings.json ("CodeBridge:Port"), or leave it empty to use the first board found.
using CodeBridge.Core;
using CodeBridge.Core.Enums;
using CodeBridge.ESP32;
using CodeBridge.Transport;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BoardConnection>();

var app = builder.Build();

app.MapGet("/", () => "CodeBridge API is running. Try POST /led/on, POST /led/off or GET /sensor/34");

app.MapPost("/led/{state}", async (string state, BoardConnection connection) =>
{
    if (state is not ("on" or "off"))
        return Results.BadRequest("Use /led/on or /led/off.");

    var board = await connection.GetAsync();
    await board.Gpio.SetPinModeAsync(2, PinMode.Output);
    await board.Gpio.DigitalWriteAsync(2, state == "on" ? PinValue.High : PinValue.Low);
    return Results.Ok(new { led = state });
});

app.MapGet("/sensor/{pin:int}", async (int pin, BoardConnection connection) =>
{
    var board = await connection.GetAsync();
    var raw = await board.Gpio.AnalogReadAsync(pin);
    return Results.Ok(new { pin, raw, volts = Math.Round(raw / 4095.0 * 3.3, 2) });
});

app.Run();

/// <summary>One board shared by every request: connects on first use, reconnects if the cable was unplugged.</summary>
internal sealed class BoardConnection : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ESP32Board? _board;

    public BoardConnection(IConfiguration configuration) => _configuration = configuration;

    public async Task<ESP32Board> GetAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_board is { IsConnected: true })
                return _board;

            if (_board is not null)
                await _board.DisposeAsync();

            var port = _configuration["CodeBridge:Port"];
            if (string.IsNullOrWhiteSpace(port))
                port = BoardDiscovery.DiscoverPorts().FirstOrDefault()?.Name
                       ?? throw new InvalidOperationException("No board found. Plug in the ESP32 or set CodeBridge:Port.");

            _board = await CodeBridgeBuilder.Connect().Serial(port).ToESP32().BuildAsync();
            return _board;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_board is not null)
            await _board.DisposeAsync();
    }
}
