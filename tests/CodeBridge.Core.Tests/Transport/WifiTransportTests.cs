using System.Net;
using System.Net.Sockets;
using CodeBridge.Transport.Wifi;

namespace CodeBridge.Core.Tests.Transport;

/// <summary>
/// Tests for WifiTransport using a local TCP server mock.
/// No ESP32 hardware required!
/// </summary>
public class WifiTransportTests : IAsyncDisposable
{
    private TcpListener? _server;
    private int _port;

    private async Task<(WifiTransport transport, TcpClient serverClient)> SetupAsync()
    {
        // Start a local TCP server on a random port
        _server = new TcpListener(IPAddress.Loopback, 0);
        _server.Start();
        _port = ((IPEndPoint)_server.LocalEndpoint).Port;

        var transport = new WifiTransport("127.0.0.1", _port);

        // Accept client and send ready signal concurrently
        var acceptTask = _server.AcceptTcpClientAsync();
        var connectTask = transport.ConnectAsync();

        var serverClient = await acceptTask;
        var writer = new StreamWriter(serverClient.GetStream()) { AutoFlush = true };
        await writer.WriteLineAsync("OK:CODEBRIDGE_READY");

        await connectTask;

        return (transport, serverClient);
    }

    [Fact]
    public async Task ConnectAsync_ReceivesReadySignal_Connects()
    {
        var (transport, serverClient) = await SetupAsync();

        Assert.True(transport.IsConnected);

        transport.Dispose();
        serverClient.Dispose();
    }

    [Fact]
    public async Task SendCommandAsync_SendsAndReceivesResponse()
    {
        var (transport, serverClient) = await SetupAsync();
        var serverStream = serverClient.GetStream();
        var serverReader = new StreamReader(serverStream);
        var serverWriter = new StreamWriter(serverStream) { AutoFlush = true };

        // Handle command in background
        var handleTask = Task.Run(async () =>
        {
            var cmd = await serverReader.ReadLineAsync();
            Assert.Equal("PING", cmd?.Trim());
            await serverWriter.WriteLineAsync("OK:PONG");
        });

        var response = await transport.SendCommandAsync("PING\n");

        await handleTask;
        Assert.Equal("OK:PONG", response);

        transport.Dispose();
        serverClient.Dispose();
    }

    [Fact]
    public async Task SendCommandAsync_MultipleCommands_AllSucceed()
    {
        var (transport, serverClient) = await SetupAsync();
        var serverStream = serverClient.GetStream();
        var serverReader = new StreamReader(serverStream);
        var serverWriter = new StreamWriter(serverStream) { AutoFlush = true };

        // Handle 3 commands in background
        var handleTask = Task.Run(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                var cmd = await serverReader.ReadLineAsync();
                await serverWriter.WriteLineAsync("OK");
            }
        });

        for (int i = 0; i < 3; i++)
        {
            var response = await transport.SendCommandAsync("DW:2:1\n");
            Assert.Equal("OK", response);
        }

        await handleTask;

        transport.Dispose();
        serverClient.Dispose();
    }

    [Fact]
    public async Task DisconnectAsync_ClosesConnection()
    {
        var (transport, serverClient) = await SetupAsync();

        await transport.DisconnectAsync();

        Assert.False(transport.IsConnected);

        serverClient.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_NoReadySignal_ThrowsException()
    {
        _server = new TcpListener(IPAddress.Loopback, 0);
        _server.Start();
        _port = ((IPEndPoint)_server.LocalEndpoint).Port;

        var transport = new WifiTransport("127.0.0.1", _port);

        var acceptTask = _server.AcceptTcpClientAsync();
        var connectTask = transport.ConnectAsync();

        var serverClient = await acceptTask;
        // Send wrong ready signal
        var writer = new StreamWriter(serverClient.GetStream()) { AutoFlush = true };
        await writer.WriteLineAsync("GARBAGE");

        await Assert.ThrowsAsync<InvalidOperationException>(() => connectTask);

        transport.Dispose();
        serverClient.Dispose();
    }

    [Fact]
    public async Task SendCommandAsync_AfterDispose_ThrowsException()
    {
        var (transport, serverClient) = await SetupAsync();

        transport.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => transport.SendCommandAsync("PING\n"));

        serverClient.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _server?.Stop();
        GC.SuppressFinalize(this);
    }
}
