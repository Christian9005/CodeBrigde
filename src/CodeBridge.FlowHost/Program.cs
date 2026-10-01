using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeBridge.Designer.WinForms;
using CodeBridge.Designer.WinForms.Hardware;
using CodeBridge.Flow;
using CodeBridge.Flow.CodeGeneration;
using CodeBridge.Flow.Execution;
using CodeBridge.Flow.Serialization;
using CodeBridge.Flow.Validation;

namespace CodeBridge.FlowHost;

/// <summary>
/// Usage: CodeBridge.FlowHost &lt;command&gt; [--option value]...
/// Commands: boards | ports | catalog | validate | export | test | upload | run
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly object OutputLock = new();

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);

        if (args.Length == 0)
        {
            Emit(new { type = "error", message = "Missing command. Use: boards, ports, catalog, validate, test, upload, run." });
            return 2;
        }

        var options = Options.Parse(args.Skip(1));
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "boards" => Boards(),
                "ports" => Ports(),
                "catalog" => Catalog(options),
                "validate" => Validate(options),
                "export" => Export(options),
                "test" => await TestAsync(options, cancellation.Token),
                "upload" => await UploadAsync(options, cancellation.Token),
                "run" => await RunAsync(options, cancellation),
                _ => Fail($"Unknown command '{args[0]}'.")
            };
        }
        catch (OperationCanceledException)
        {
            Emit(new { type = "result", success = false, cancelled = true, message = "Cancelled." });
            return 1;
        }
        catch (Exception ex)
        {
            var profile = ResolveBoard(options, required: false);
            Emit(new
            {
                type = "error",
                message = Describe(ex, profile, options)
            });
            return 1;
        }
    }

    private static string Describe(Exception ex, BoardProfile? profile, Options options)
    {
        var port = options.Get("port") ?? options.Get("host") ?? "the selected port";
        switch (ex)
        {
            case FileNotFoundException or DirectoryNotFoundException:
                return $"{port} was not found. Check the USB cable and pick the board's COM port again (use the refresh button).";
            case UnauthorizedAccessException:
                return $"{port} is in use by another program (Serial Monitor, Arduino IDE, another Visual Studio window...). Close it and try again.";
            case IOException when ex.Message.Contains("semaphore", StringComparison.OrdinalIgnoreCase):
                return $"{port} stopped responding. Unplug and reconnect the board, then try again.";
        }

        return profile is null ? ex.Message : BoardConnectionService.FormatConnectionFailure(ex, profile, options.Get("port"));
    }

    // ---------------------------------------------------------------- boards / ports

    private static int Boards()
    {
        var boards = BuiltInBoardProfiles.All.Select(board => new
        {
            id = board.Id,
            displayName = board.DisplayName,
            canFlash = HardwareToolLocator.ResolveFirmwareDirectory(board.Id) is not null,
            flashTool = string.Equals(board.Id, BuiltInBoardProfiles.ArduinoUno.Id, StringComparison.OrdinalIgnoreCase)
                ? "Arduino CLI"
                : "esptool"
        });

        Emit(new { type = "boards", boards });
        return 0;
    }

    private static int Ports()
    {
        var ports = System.IO.Ports.SerialPort.GetPortNames()
            .OrderBy(name => name.Length)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new
            {
                name,
                description = BoardPortDetector.DescribePort(name),
                suggestedBoard = BoardPortDetector.SuggestBoardForPort(name)?.Id
            });

        Emit(new { type = "ports", ports });
        return 0;
    }

    // ---------------------------------------------------------------- catalog

    private static int Catalog(Options options)
    {
        var outDirectory = options.Get("out");
        foreach (var board in BuiltInBoardProfiles.All)
        {
            var catalog = BuiltInBlockCatalog.Create(board);
            var payload = new
            {
                boardId = board.Id,
                displayName = board.DisplayName,
                blocks = catalog.Blocks
                    .OrderBy(block => block.Category, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(block => block.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Select(block => new
                    {
                        type = block.Type,
                        displayName = block.DisplayName,
                        category = block.Category,
                        description = block.Description,
                        ports = block.Ports.Select(port => new
                        {
                            name = port.Name,
                            direction = port.Direction,
                            valueKind = port.ValueKind,
                            required = port.Required
                        }),
                        properties = block.Properties.Select(property => new
                        {
                            name = property.Name,
                            valueKind = property.ValueKind,
                            required = property.Required,
                            isAdvanced = property.IsAdvanced,
                            defaultValue = property.DefaultValue,
                            options = property.Options?.Select(option => new
                            {
                                label = option.Label,
                                value = option.Value,
                                description = option.Description
                            })
                        })
                    })
            };

            var text = JsonSerializer.Serialize(payload, new JsonSerializerOptions(Json) { WriteIndented = true });
            if (string.IsNullOrWhiteSpace(outDirectory))
            {
                Console.WriteLine(text);
            }
            else
            {
                Directory.CreateDirectory(outDirectory);
                File.WriteAllText(Path.Combine(outDirectory, $"catalog.{board.Id}.json"), text + "\n", new UTF8Encoding(false));
            }
        }

        return 0;
    }

    // ---------------------------------------------------------------- validate

    private static int Validate(Options options)
    {
        var document = LoadDocument(options);
        var profile = BuiltInBoardProfiles.FindById(options.Get("board") ?? document.BoardId) ?? BuiltInBoardProfiles.Default;
        var result = new FlowValidator().Validate(document, BuiltInBlockCatalog.Create(profile));

        Emit(new
        {
            type = "validation",
            isValid = result.IsValid,
            issues = result.Issues.Select(issue => new
            {
                severity = issue.Severity,
                message = issue.Message,
                nodeId = issue.NodeId,
                connectionId = issue.ConnectionId
            })
        });
        return result.IsValid ? 0 : 1;
    }

    // ---------------------------------------------------------------- export to C#

    private static int Export(Options options)
    {
        var document = LoadDocument(options);
        if (options.Get("board") is { } board)
            document.BoardId = board;

        var mode = string.Equals(options.Get("mode"), "console", StringComparison.OrdinalIgnoreCase)
            ? FlowCSharpMode.ConsoleApp
            : FlowCSharpMode.Class;

        var result = FlowCSharpGenerator.Generate(document, new FlowCSharpOptions
        {
            ClassName = options.Get("class"),
            Namespace = options.Get("namespace") ?? "CodeBridge.Flows",
            Mode = mode,
            Loop = options.Has("loop"),
            LoopIntervalMs = Math.Max(50, options.GetInt("interval", 1000))
        });

        Emit(new { type = "export", className = result.ClassName, mode = mode.ToString(), code = result.Code, warnings = result.Warnings });
        return 0;
    }

    // ---------------------------------------------------------------- test connection

    private static async Task<int> TestAsync(Options options, CancellationToken ct)
    {
        var profile = ResolveBoard(options)!;
        Emit(new { type = "status", state = "connecting", message = $"Connecting to {profile.DisplayName}..." });

        var board = await BoardConnectionService.ConnectAsync(CreateRequest(profile, options), ct);
        try
        {
            object? info = null;
            try
            {
                var boardInfo = await board.GetInfoAsync(ct);
                info = boardInfo;
            }
            catch (Exception)
            {
                // Board info is optional (the Arduino Uno sketch does not implement it).
            }

            Emit(new { type = "connected", board = profile.Id, name = board.Name, firmware = board.FirmwareVersion, info });
            return 0;
        }
        finally
        {
            await BoardConnectionService.DisposeBoardAsync(board);
        }
    }

    // ---------------------------------------------------------------- upload firmware

    private static async Task<int> UploadAsync(Options options, CancellationToken ct)
    {
        var profile = ResolveBoard(options)!;
        var port = options.Get("port") ?? throw new InvalidOperationException("Select a serial port before uploading firmware.");

        var uploader = FirmwareUploaderFactory.Create(new FirmwareUploadRequest(profile, port));
        Emit(new { type = "status", state = "uploading", message = $"Uploading {profile.DisplayName} firmware to {port} with {uploader.ToolName}..." });
        uploader.OutputReceived += line => Emit(new { type = "log", text = line });

        var result = await uploader.UploadAsync(ct);
        Emit(new
        {
            type = "result",
            success = result.Success,
            message = result.Success
                ? $"Firmware uploaded to {profile.DisplayName} on {port}."
                : $"Firmware upload failed (exit code {result.ExitCode?.ToString() ?? "n/a"}).",
            exitCode = result.ExitCode
        });
        return result.Success ? 0 : 1;
    }

    // ---------------------------------------------------------------- run flow

    private static async Task<int> RunAsync(Options options, CancellationTokenSource cancellation)
    {
        var document = LoadDocument(options);
        var profile = BuiltInBoardProfiles.FindById(options.Get("board") ?? document.BoardId) ?? BuiltInBoardProfiles.Default;
        var catalog = BuiltInBlockCatalog.Create(profile);
        var ct = cancellation.Token;

        var validation = new FlowValidator().Validate(document, catalog);
        foreach (var issue in validation.Issues)
            Emit(new { type = "issue", severity = issue.Severity, message = issue.Message, nodeId = issue.NodeId, connectionId = issue.ConnectionId });

        if (!validation.IsValid)
        {
            Emit(new { type = "result", success = false, message = "The flow has errors. Fix them and run again." });
            return 1;
        }

        // The extension asks for a graceful stop by writing "stop" to stdin (or by closing it).
        _ = Task.Run(async () =>
        {
            while (await Console.In.ReadLineAsync() is { } line)
            {
                if (line.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase))
                    break;
            }

            cancellation.Cancel();
        });

        Emit(new { type = "status", state = "connecting", message = $"Connecting to {profile.DisplayName}..." });
        var board = await BoardConnectionService.ConnectAsync(CreateRequest(profile, options), ct);

        try
        {
            Emit(new { type = "connected", board = profile.Id, name = board.Name, firmware = board.FirmwareVersion });
            Emit(new { type = "status", state = "running", message = "Running flow..." });

            var loop = options.Has("loop");
            var interval = TimeSpan.FromMilliseconds(Math.Max(50, options.GetInt("interval", 1000)));
            var traceDelay = TimeSpan.FromMilliseconds(Math.Max(0, options.GetInt("trace-ms", 150)));
            var runtime = new FlowRuntime(catalog);
            var iteration = 0;

            do
            {
                iteration++;
                Emit(new { type = "iteration", number = iteration });

                await runtime.ExecuteAsync(
                    document,
                    new FlowExecutionContext
                    {
                        Board = board,
                        TraceDelay = traceDelay,
                        EventHandler = (executionEvent, _) =>
                        {
                            Emit(new
                            {
                                type = "node",
                                kind = executionEvent.Kind,
                                nodeId = executionEvent.NodeId,
                                blockType = executionEvent.BlockType,
                                message = executionEvent.Message,
                                outputs = executionEvent.Outputs?.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString())
                            });
                            return ValueTask.CompletedTask;
                        }
                    },
                    ct);

                if (loop)
                    await Task.Delay(interval, ct);
            }
            while (loop && !ct.IsCancellationRequested);

            Emit(new
            {
                type = "result",
                success = true,
                message = loop
                    ? "Flow stopped."
                    : "Flow completed. The board restarts when the USB connection closes, so outputs return to their default state; use Loop to keep a flow running."
            });
            return 0;
        }
        catch (OperationCanceledException)
        {
            Emit(new { type = "result", success = true, cancelled = true, message = "Flow stopped." });
            return 0;
        }
        finally
        {
            await BoardConnectionService.DisposeBoardAsync(board);
        }
    }

    // ---------------------------------------------------------------- helpers

    private static FlowDocument LoadDocument(Options options)
    {
        var path = options.Get("flow") ?? throw new InvalidOperationException("Missing --flow <file.cbflow>.");
        return FlowDocumentJson.Deserialize(File.ReadAllText(path));
    }

    private static BoardProfile? ResolveBoard(Options options, bool required = true)
    {
        var id = options.Get("board");
        var profile = BuiltInBoardProfiles.FindById(id);
        if (profile is null && required)
            throw new InvalidOperationException($"Unknown board '{id}'. Available: {string.Join(", ", BuiltInBoardProfiles.All.Select(b => b.Id))}.");

        return profile;
    }

    private static BoardConnectionRequest CreateRequest(BoardProfile profile, Options options)
    {
        var target = options.Get("port") ?? options.Get("host")
            ?? throw new InvalidOperationException("Select a serial port (or enter the board's IP address) before connecting.");

        // "COM3" (or /dev/tty*) is USB serial; anything else is treated as the board's Wi-Fi host name or IP.
        var isSerial = target.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || target.StartsWith("/dev/", StringComparison.Ordinal);
        return new BoardConnectionRequest(
            profile,
            isSerial ? CodeBridgeTransportMode.Serial : CodeBridgeTransportMode.WiFi,
            isSerial ? target : string.Empty,
            options.GetInt("baud", 115200),
            isSerial ? string.Empty : target,
            options.GetInt("tcp-port", 8080));
    }

    private static int Fail(string message)
    {
        Emit(new { type = "error", message });
        return 2;
    }

    private static void Emit(object value)
    {
        var line = JsonSerializer.Serialize(value, Json);
        lock (OutputLock)
        {
            Console.Out.WriteLine(line);
            Console.Out.Flush();
        }
    }

    private sealed class Options
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        public static Options Parse(IEnumerable<string> args)
        {
            var options = new Options();
            var list = args.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (!list[i].StartsWith("--", StringComparison.Ordinal))
                    continue;

                var key = list[i][2..];
                var hasValue = i + 1 < list.Count && !list[i + 1].StartsWith("--", StringComparison.Ordinal);
                options._values[key] = hasValue ? list[++i] : "true";
            }

            return options;
        }

        public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

        public bool Has(string key) => _values.ContainsKey(key);

        public int GetInt(string key, int fallback) =>
            int.TryParse(Get(key), out var value) ? value : fallback;
    }
}
