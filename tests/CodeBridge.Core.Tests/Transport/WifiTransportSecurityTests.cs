using System.Net;
using System.Net.Sockets;
using System.Text;
using CodeBridge.Transport.Wifi;

namespace CodeBridge.Core.Tests.Transport;

/// <summary>
/// WifiTransport against a scripted fake of the 0.9 firmware: pairing token, oversized lines and late responses.
/// </summary>
public class WifiTransportSecurityTests : IDisposable
{
    private readonly TcpListener _server = new(IPAddress.Loopback, 0);

    public WifiTransportSecurityTests()
    {
        _server.Start();
    }

    private int Port => ((IPEndPoint)_server.LocalEndpoint).Port;

    /// <summary>Plays the board: sends READY, then answers each received line using <paramref name="reply"/> (null = stay silent).</summary>
    private Task RunBoard(Func<string, string?> reply, Action<StreamWriter>? onConnected = null) => Task.Run(async () =>
    {
        using var client = await _server.AcceptTcpClientAsync();
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };
        await writer.WriteLineAsync("OK:CODEBRIDGE_READY");
        onConnected?.Invoke(writer);

        try
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                var answer = reply(line.Trim());
                if (answer is not null)
                    await writer.WriteLineAsync(answer);
            }
        }
        catch (IOException) { }
    });

    [Fact]
    public async Task The_token_is_sent_as_the_first_command_and_commands_then_work()
    {
        var received = new List<string>();
        var board = RunBoard(line =>
        {
            lock (received) received.Add(line);
            return line.StartsWith("AUTH:") ? (line == "AUTH:secret-token-123" ? "OK" : "ERR:AUTH_FAILED")
                : line == "PING" ? "OK:PONG" : "ERR:Unknown command";
        });

        using var transport = new WifiTransport("127.0.0.1", Port, "secret-token-123");
        await transport.ConnectAsync();

        var response = await transport.SendCommandAsync("PING\n");

        Assert.Equal("OK:PONG", response);
        lock (received) Assert.Equal("AUTH:secret-token-123", received[0]);
        transport.Dispose();
        await Task.WhenAny(board, Task.Delay(1000));
    }

    [Fact]
    public async Task A_wrong_token_fails_the_connection_without_retrying()
    {
        var attempts = 0;
        var board = RunBoard(line =>
        {
            Interlocked.Increment(ref attempts);
            return "ERR:AUTH_FAILED";
        });

        using var transport = new WifiTransport("127.0.0.1", Port, "wrong-token-000") { RetryDelay = TimeSpan.FromMilliseconds(10) };

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => transport.ConnectAsync());

        Assert.Contains("pairing token", error.Message);
        Assert.Equal(1, attempts);
        await Task.WhenAny(board, Task.Delay(1000));
    }

    [Fact]
    public async Task A_board_that_needs_a_token_gives_a_clear_error_when_none_was_supplied()
    {
        var board = RunBoard(_ => "ERR:NO_TOKEN: pair the board over USB first");

        using var transport = new WifiTransport("127.0.0.1", Port);
        await transport.ConnectAsync();

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => transport.SendCommandAsync("PING\n"));

        Assert.Contains("Pair it over USB", error.Message);
        await Task.WhenAny(board, Task.Delay(1000));
    }

    [Fact]
    public async Task A_line_longer_than_the_limit_is_dropped_and_the_connection_keeps_working()
    {
        var board = RunBoard(line => line == "PING" ? "OK:PONG" : null, writer =>
        {
            // 1 MB without a newline, then a normal protocol line: the flood must not be buffered forever.
            writer.Write(new string('x', 1_000_000));
            writer.Write("\n");
        });

        using var transport = new WifiTransport("127.0.0.1", Port);
        await transport.ConnectAsync();

        var response = await transport.SendCommandAsync("PING\n");

        Assert.Equal("OK:PONG", response);
        await Task.WhenAny(board, Task.Delay(1000));
    }

    [Fact]
    public async Task A_late_answer_to_a_timed_out_command_is_not_taken_for_the_next_command()
    {
        var pings = 0;
        var board = RunBoard(line =>
        {
            if (line.StartsWith("AR:"))
                return null; // the board is busy and answers late (below)
            if (line == "PING")
                return Interlocked.Increment(ref pings) == 1 ? "OK:1234" /* stale answer to AR */ + "\nOK:PONG" : "OK:PONG";
            if (line.StartsWith("DR:"))
                return "OK:1";
            return "ERR:Unknown command";
        });

        using var transport = new WifiTransport("127.0.0.1", Port);
        await transport.ConnectAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.SendCommandAsync("AR:34\n", cts.Token));

        // The next command resynchronizes first (PING until PONG) and only then sends DR:
        var response = await transport.SendCommandAsync("DR:4\n");

        Assert.Equal("OK:1", response);
        await Task.WhenAny(board, Task.Delay(1000));
    }

    public void Dispose()
    {
        try { _server.Stop(); } catch { }
        GC.SuppressFinalize(this);
    }
}
