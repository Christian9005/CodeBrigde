using CodeBridge.Flow;
using CodeBridge.Flow.Execution;
using CodeBridge.Core.Tests.Mocks;
using CodeBridge.ESP32;

namespace CodeBridge.Core.Tests.Flow;

public class FlowRuntimeTests
{
    [Fact]
    public async Task ExecuteAsync_CompareFlow_ProducesResult()
    {
        var document = new FlowDocument
        {
            Name = "Compare",
            Nodes =
            {
                new FlowNode
                {
                    Id = "left",
                    Type = BuiltInBlockCatalog.ConstantNumber,
                    Parameters = { ["value"] = 42 }
                },
                new FlowNode
                {
                    Id = "right",
                    Type = BuiltInBlockCatalog.ConstantNumber,
                    Parameters = { ["value"] = 10 }
                },
                new FlowNode
                {
                    Id = "compare",
                    Type = BuiltInBlockCatalog.Compare,
                    Parameters = { ["operator"] = ">" }
                }
            },
            Connections =
            {
                new FlowConnection
                {
                    Id = "left-compare",
                    FromNodeId = "left",
                    FromPort = "value",
                    ToNodeId = "compare",
                    ToPort = "left"
                },
                new FlowConnection
                {
                    Id = "right-compare",
                    FromNodeId = "right",
                    FromPort = "value",
                    ToNodeId = "compare",
                    ToPort = "right"
                }
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        var result = await runtime.ExecuteAsync(document);

        Assert.True(result.GetOutput<bool>("compare", "result"));
    }

    [Fact]
    public async Task ExecuteAsync_TimerWithZeroInterval_ProducesTick()
    {
        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode
                {
                    Id = "timer",
                    Type = BuiltInBlockCatalog.Timer,
                    Parameters = { ["intervalMs"] = 0 }
                }
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        var result = await runtime.ExecuteAsync(document);

        Assert.True(result.GetOutput<bool>("timer", "tick"));
    }

    [Fact]
    public async Task ExecuteAsync_ManualTriggerChain_ProducesDone()
    {
        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode { Id = "run", Type = BuiltInBlockCatalog.ManualTrigger },
                new FlowNode
                {
                    Id = "timer",
                    Type = BuiltInBlockCatalog.Timer,
                    Parameters = { ["intervalMs"] = 0 }
                }
            },
            Connections =
            {
                new FlowConnection
                {
                    Id = "run-timer",
                    FromNodeId = "run",
                    FromPort = "trigger",
                    ToNodeId = "timer",
                    ToPort = "trigger"
                }
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        var result = await runtime.ExecuteAsync(document);

        Assert.True(result.GetOutput<bool>("run", "trigger"));
        Assert.True(result.GetOutput<bool>("timer", "tick"));
    }

    [Fact]
    public async Task ExecuteAsync_ReportsNodeExecutionEvents()
    {
        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode { Id = "run", Type = BuiltInBlockCatalog.ManualTrigger },
                new FlowNode
                {
                    Id = "timer",
                    Type = BuiltInBlockCatalog.Timer,
                    Parameters = { ["intervalMs"] = 0 }
                }
            },
            Connections =
            {
                new FlowConnection
                {
                    Id = "run-timer",
                    FromNodeId = "run",
                    FromPort = "trigger",
                    ToNodeId = "timer",
                    ToPort = "trigger"
                }
            }
        };

        var events = new List<(FlowExecutionEventKind Kind, string NodeId)>();
        var context = new FlowExecutionContext
        {
            EventHandler = (executionEvent, _) =>
            {
                events.Add((executionEvent.Kind, executionEvent.NodeId));
                return ValueTask.CompletedTask;
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        await runtime.ExecuteAsync(document, context);

        Assert.Equal(
            [
                (FlowExecutionEventKind.NodeStarted, "run"),
                (FlowExecutionEventKind.NodeCompleted, "run"),
                (FlowExecutionEventKind.NodeStarted, "timer"),
                (FlowExecutionEventKind.NodeCompleted, "timer")
            ],
            events);
    }

    [Fact]
    public async Task ExecuteAsync_GpioBlockWithoutBoard_Throws()
    {
        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode
                {
                    Id = "read",
                    Type = BuiltInBlockCatalog.GpioDigitalRead,
                    Parameters = { ["pin"] = 2 }
                }
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runtime.ExecuteAsync(document));

        Assert.Contains("requires a connected board", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TriggeredDigitalWrite_SendsCommandToBoard()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.7.0", "OK");

        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode { Id = "run", Type = BuiltInBlockCatalog.ManualTrigger },
                new FlowNode
                {
                    Id = "value",
                    Type = BuiltInBlockCatalog.ConstantBoolean,
                    Parameters = { ["value"] = true }
                },
                new FlowNode
                {
                    Id = "write",
                    Type = BuiltInBlockCatalog.GpioDigitalWrite,
                    Parameters = { ["pin"] = 2 }
                }
            },
            Connections =
            {
                new FlowConnection
                {
                    Id = "run-write",
                    FromNodeId = "run",
                    FromPort = "trigger",
                    ToNodeId = "write",
                    ToPort = "trigger"
                },
                new FlowConnection
                {
                    Id = "value-write",
                    FromNodeId = "value",
                    FromPort = "value",
                    ToNodeId = "write",
                    ToPort = "value"
                }
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        var result = await runtime.ExecuteAsync(document, board);

        Assert.Contains("DW:2:1\n", transport.SentCommands);
        Assert.True(result.GetOutput<bool>("write", "done"));
    }

    [Fact]
    public async Task ExecuteAsync_PinModeThenDigitalWrite_SendsModeBeforeWrite()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.7.0", "OK", "OK");

        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode { Id = "run", Type = BuiltInBlockCatalog.ManualTrigger },
                new FlowNode
                {
                    Id = "mode",
                    Type = BuiltInBlockCatalog.GpioPinMode,
                    Parameters = { ["pin"] = 2, ["mode"] = "Output" }
                },
                new FlowNode
                {
                    Id = "write",
                    Type = BuiltInBlockCatalog.GpioDigitalWrite,
                    Parameters = { ["pin"] = 2, ["value"] = true }
                }
            },
            Connections =
            {
                new FlowConnection
                {
                    Id = "run-mode",
                    FromNodeId = "run",
                    FromPort = "trigger",
                    ToNodeId = "mode",
                    ToPort = "trigger"
                },
                new FlowConnection
                {
                    Id = "mode-write",
                    FromNodeId = "mode",
                    FromPort = "done",
                    ToNodeId = "write",
                    ToPort = "trigger"
                }
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        await runtime.ExecuteAsync(document, board);

        Assert.Contains("PM:2:1\n", transport.SentCommands);
        Assert.Contains("DW:2:1\n", transport.SentCommands);
        var sentCommands = transport.SentCommands.ToList();
        Assert.True(
            sentCommands.IndexOf("PM:2:1\n") <
            sentCommands.IndexOf("DW:2:1\n"));
    }

    [Fact]
    public async Task ExecuteAsync_DebugLog_EmitsMessageFromInputValue()
    {
        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode
                {
                    Id = "value",
                    Type = BuiltInBlockCatalog.ConstantBoolean,
                    Parameters = { ["value"] = true }
                },
                new FlowNode
                {
                    Id = "debug",
                    Type = BuiltInBlockCatalog.DebugLog,
                    Parameters = { ["label"] = "GPIO 2" }
                }
            },
            Connections =
            {
                new FlowConnection
                {
                    Id = "value-debug",
                    FromNodeId = "value",
                    FromPort = "value",
                    ToNodeId = "debug",
                    ToPort = "value"
                }
            }
        };

        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        var result = await runtime.ExecuteAsync(document);

        Assert.Equal("GPIO 2: true", result.GetOutput<string>("debug", "message"));
        Assert.True(result.GetOutput<bool>("debug", "done"));
    }

    [Fact]
    public async Task ExecuteAsync_CustomHandler_CanHandleFutureBlocks()
    {
        var catalog = BuiltInBlockCatalog.Create()
            .Register(new FlowBlockDefinition
            {
                Type = "test.echo",
                DisplayName = "Echo",
                Ports =
                [
                    FlowPortDefinition.Output("value", FlowValueKind.String)
                ],
                Properties =
                [
                    new FlowPropertyDefinition("value", FlowValueKind.String, DefaultValue: "hello")
                ]
            });

        var document = new FlowDocument
        {
            Nodes =
            {
                new FlowNode
                {
                    Id = "echo",
                    Type = "test.echo",
                    Parameters = { ["value"] = "CodeBridge" }
                }
            }
        };

        var runtime = new FlowRuntime(catalog)
            .RegisterHandler("test.echo", context =>
                ValueTask.FromResult<IReadOnlyDictionary<string, object?>>(
                    new Dictionary<string, object?> { ["value"] = context.GetParameter<string>("value") }));

        var result = await runtime.ExecuteAsync(document);

        Assert.Equal("CodeBridge", result.GetOutput<string>("echo", "value"));
    }
}
