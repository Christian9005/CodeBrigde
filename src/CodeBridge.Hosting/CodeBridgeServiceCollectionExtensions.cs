using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeBridge.Hosting;

/// <summary>Registration of the shared CodeBridge board connection.</summary>
public static class CodeBridgeServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="BoardService"/> as a singleton and, when <see cref="CodeBridgeOptions.AutoConnect"/> is on, connects at startup
    /// without blocking it (a missing board never prevents the app from starting).
    /// <code>builder.Services.AddCodeBridge(o =&gt; o.Port = "simulator");</code>
    /// </summary>
    public static IServiceCollection AddCodeBridge(this IServiceCollection services, Action<CodeBridgeOptions>? configure = null)
    {
        var options = services.AddOptions<CodeBridgeOptions>();
        if (configure is not null)
            options.Configure(configure);

        services.AddSingleton<BoardService>();
        services.AddHostedService<BoardConnectionHostedService>();
        return services;
    }

    /// <summary>Same as <see cref="AddCodeBridge(IServiceCollection, Action{CodeBridgeOptions}?)"/>, reading the <c>"CodeBridge"</c> configuration section first.</summary>
    public static IServiceCollection AddCodeBridge(this IServiceCollection services, IConfiguration configuration, Action<CodeBridgeOptions>? configure = null)
    {
        services.AddOptions<CodeBridgeOptions>().Bind(configuration.GetSection("CodeBridge"));
        return services.AddCodeBridge(configure);
    }
}

/// <summary>Connects the board in the background when the host starts and disconnects when it stops.</summary>
internal sealed class BoardConnectionHostedService : IHostedService
{
    private readonly BoardService _board;
    private readonly CodeBridgeOptions _options;
    private readonly ILogger<BoardConnectionHostedService> _logger;

    public BoardConnectionHostedService(BoardService board, IOptions<CodeBridgeOptions> options, ILogger<BoardConnectionHostedService> logger)
    {
        _board = board;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.AutoConnect)
        {
            // Not awaited: starting the web host must not wait for a cable or a Wi-Fi handshake.
            _ = Task.Run(async () =>
            {
                try
                {
                    await _board.ConnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("CodeBridge board not available at startup: {Message}", ex.Message);
                }
            }, CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => _board.DisconnectAsync();
}
