using CodeBridge.Flow;
using CodeBridge.Flow.Execution;
using CodeBridge.Flow.Serialization;

namespace CodeBridge.Core.Tests.Flow;

public class FlowDocumentJsonTests
{
    [Fact]
    public async Task SerializeDeserialize_RoundTripsExecutableFlow()
    {
        var document = new FlowDocument
        {
            Name = "Round Trip",
            Nodes =
            {
                new FlowNode
                {
                    Id = "left",
                    Type = BuiltInBlockCatalog.ConstantNumber,
                    Parameters = { ["value"] = 5 }
                },
                new FlowNode
                {
                    Id = "right",
                    Type = BuiltInBlockCatalog.ConstantNumber,
                    Parameters = { ["value"] = 5 }
                },
                new FlowNode
                {
                    Id = "compare",
                    Type = BuiltInBlockCatalog.Compare,
                    Parameters = { ["operator"] = "==" }
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

        var json = FlowDocumentJson.Serialize(document);
        var restored = FlowDocumentJson.Deserialize(json);
        var runtime = new FlowRuntime(BuiltInBlockCatalog.Create());

        var result = await runtime.ExecuteAsync(restored);

        Assert.Equal("Round Trip", restored.Name);
        Assert.True(result.GetOutput<bool>("compare", "result"));
    }
}

