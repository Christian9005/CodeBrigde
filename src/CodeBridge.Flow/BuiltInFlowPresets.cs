namespace CodeBridge.Flow;

/// <summary>
/// Ready-to-run flow documents used by designer surfaces, samples, and Visual Studio smart tags.
/// </summary>
public static class BuiltInFlowPresets
{
    public static FlowDocument CreateBlinkOnce(int pin = 2, int delayMs = 1500, bool activeLow = false)
    {
        return new FlowDocument
        {
            Name = activeLow ? $"Blink GPIO {pin} Once Active Low" : $"Blink GPIO {pin} Once",
            Nodes =
            {
                new FlowNode { Id = "run", Type = BuiltInBlockCatalog.ManualTrigger, Position = new FlowPosition(70, 100) },
                new FlowNode { Id = "blink", Type = BuiltInBlockCatalog.GpioBlinkLed, Position = new FlowPosition(360, 100), Parameters = { ["pin"] = pin, ["durationMs"] = delayMs, ["activeLow"] = activeLow, ["leaveOn"] = false } }
            },
            Connections =
            {
                Connect("run", "trigger", "blink", "trigger")
            }
        };
    }

    public static FlowDocument CreateDigitalReadDebug(int pin = 2)
    {
        return new FlowDocument
        {
            Name = $"Read GPIO {pin} Debug",
            Nodes =
            {
                new FlowNode { Id = "run", Type = BuiltInBlockCatalog.ManualTrigger, Position = new FlowPosition(50, 80) },
                new FlowNode { Id = "pin-mode", Type = BuiltInBlockCatalog.GpioPinMode, Position = new FlowPosition(350, 80), Parameters = { ["pin"] = pin, ["mode"] = "Input" } },
                new FlowNode { Id = "read", Type = BuiltInBlockCatalog.GpioDigitalRead, Position = new FlowPosition(350, 240), Parameters = { ["pin"] = pin } },
                new FlowNode { Id = "debug", Type = BuiltInBlockCatalog.DebugLog, Position = new FlowPosition(650, 240), Parameters = { ["label"] = $"GPIO {pin}" } }
            },
            Connections =
            {
                Connect("run", "trigger", "pin-mode", "trigger"),
                Connect("pin-mode", "done", "read", "trigger"),
                Connect("pin-mode", "done", "debug", "trigger"),
                Connect("read", "value", "debug", "value")
            }
        };
    }

    public static FlowDocument CreateAnalogStreamDashboard(
        int pin = 32,
        int sampleRateHz = 1000,
        int bufferCapacity = 4096,
        int batchSize = 64)
    {
        return new FlowDocument
        {
            Name = $"Analog Stream GPIO {pin}",
            Nodes =
            {
                new FlowNode { Id = "run", Type = BuiltInBlockCatalog.ManualTrigger, Position = new FlowPosition(50, 90) },
                new FlowNode
                {
                    Id = "sample",
                    Type = BuiltInBlockCatalog.SampleChannel,
                    Position = new FlowPosition(350, 90),
                    Parameters =
                    {
                        ["pin"] = pin,
                        ["analog"] = true,
                        ["mode"] = "HardwareTimer",
                        ["sampleRateHz"] = sampleRateHz,
                        ["bufferCapacity"] = bufferCapacity,
                        ["backpressure"] = "DropOldest",
                        ["batchSize"] = batchSize
                    }
                },
                new FlowNode
                {
                    Id = "dashboard",
                    Type = BuiltInBlockCatalog.StreamDashboard,
                    Position = new FlowPosition(650, 90),
                    Parameters =
                    {
                        ["channelName"] = $"GPIO {pin}",
                        ["refreshHz"] = 20,
                        ["retentionSeconds"] = 30,
                        ["maxPoints"] = batchSize
                    }
                },
                new FlowNode
                {
                    Id = "debug",
                    Type = BuiltInBlockCatalog.DebugLog,
                    Position = new FlowPosition(650, 250),
                    Parameters = { ["label"] = $"GPIO {pin} stream" }
                }
            },
            Connections =
            {
                Connect("run", "trigger", "sample", "trigger"),
                Connect("sample", "samples", "dashboard", "samples"),
                Connect("run", "trigger", "debug", "trigger"),
                Connect("sample", "status", "debug", "value")
            }
        };
    }

    private static FlowConnection Connect(
        string fromNodeId,
        string fromPort,
        string toNodeId,
        string toPort) => new()
    {
        Id = $"{fromNodeId}-{toNodeId}-{toPort}",
        FromNodeId = fromNodeId,
        FromPort = fromPort,
        ToNodeId = toNodeId,
        ToPort = toPort
    };
}
