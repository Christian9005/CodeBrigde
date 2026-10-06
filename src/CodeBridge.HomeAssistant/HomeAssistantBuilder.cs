using CodeBridge.Core.Enums;
using CodeBridge.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBridge.HomeAssistant;

/// <summary>Describes what the board shows in Home Assistant.</summary>
public sealed class HomeAssistantBuilder
{
    private readonly List<HomeAssistantEntity> _entities = new();

    internal IReadOnlyList<HomeAssistantEntity> Entities => _entities;

    /// <summary>Adds any entity, including your own subclasses of <see cref="HomeAssistantEntity"/>.</summary>
    public HomeAssistantBuilder Add(HomeAssistantEntity entity)
    {
        _entities.Add(entity);
        return this;
    }

    /// <summary>A digital output (relay, lamp, LED) as an on/off switch.</summary>
    public HomeAssistantBuilder Switch(string id, string name, int pin, bool activeLow = false, bool initialOn = false) =>
        Add(new SwitchEntity(id, name, pin, activeLow, initialOn));

    /// <summary>A PWM output as a dimmable light.</summary>
    public HomeAssistantBuilder Light(string id, string name, int pin, int frequencyHz = 5000) =>
        Add(new LightEntity(id, name, pin, frequencyHz));

    /// <summary>
    /// An analog pin as a sensor. The raw reading (0 to the board's ADC range) is multiplied by <paramref name="scale"/>:
    /// pass <c>100.0 / 4095</c> for a percentage on an ESP32.
    /// </summary>
    public HomeAssistantBuilder AnalogSensor(string id, string name, int pin, string? unit = null, string? deviceClass = null,
        double scale = 1.0, double offset = 0.0, double intervalSeconds = 5, double deadband = 0, int decimals = 1) =>
        Add(new SensorEntity(id, name, async (board, ct) => await board.ReadAnalogAsync(pin, ct) * scale + offset,
            unit, deviceClass, TimeSpan.FromSeconds(intervalSeconds), deadband, decimals));

    /// <summary>Any number you can read from the board (a DHT temperature, a distance, an INA219 current...).</summary>
    public HomeAssistantBuilder Sensor(string id, string name, Func<BoardService, CancellationToken, Task<double>> read,
        string? unit = null, string? deviceClass = null, double intervalSeconds = 10, double deadband = 0, int decimals = 1) =>
        Add(new SensorEntity(id, name, read, unit, deviceClass, TimeSpan.FromSeconds(intervalSeconds), deadband, decimals));

    /// <summary>A digital input (button, door contact, PIR) as a binary sensor.</summary>
    public HomeAssistantBuilder BinarySensor(string id, string name, int pin, string? deviceClass = null, bool invert = false, double intervalSeconds = 1) =>
        Add(new BinarySensorEntity(id, name, async (board, ct) => await board.ReadDigitalAsync(pin, ct) != invert, deviceClass, TimeSpan.FromSeconds(intervalSeconds)));

    /// <summary>Any on/off condition you can compute from the board.</summary>
    public HomeAssistantBuilder BinarySensor(string id, string name, Func<BoardService, CancellationToken, Task<bool>> read, string? deviceClass = null, double intervalSeconds = 1) =>
        Add(new BinarySensorEntity(id, name, read, deviceClass, TimeSpan.FromSeconds(intervalSeconds)));
}

/// <summary>Registration of the Home Assistant bridge.</summary>
public static class HomeAssistantServiceCollectionExtensions
{
    /// <summary>
    /// Shows the board in Home Assistant through MQTT discovery. Needs <c>AddCodeBridge()</c> too.
    /// <code>
    /// services.AddCodeBridge(o =&gt; o.Port = "simulator");
    /// services.AddCodeBridgeHomeAssistant(o =&gt; { o.Broker = "192.168.1.10"; o.Username = "codebridge"; o.Password = "..."; },
    ///     ha =&gt; ha.Switch("led", "Built-in LED", pin: 2).AnalogSensor("light", "Light", pin: 34, unit: "%", scale: 100.0 / 4095));
    /// </code>
    /// </summary>
    public static IServiceCollection AddCodeBridgeHomeAssistant(this IServiceCollection services, Action<HomeAssistantOptions> configure, Action<HomeAssistantBuilder> entities)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(entities);

        services.AddOptions<HomeAssistantOptions>().Configure(configure);

        var builder = new HomeAssistantBuilder();
        entities(builder);
        foreach (var entity in builder.Entities)
            services.AddSingleton(entity);

        services.TryAddSingleton<BoardService>();
        services.AddSingleton<HomeAssistantBridge>();
        services.AddHostedService(provider => provider.GetRequiredService<HomeAssistantBridge>());
        return services;
    }
}
