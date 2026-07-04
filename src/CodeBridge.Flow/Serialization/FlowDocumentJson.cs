using System.Text.Json;
using System.Text.Json.Serialization;
using CodeBridge.Flow;

namespace CodeBridge.Flow.Serialization;

public static class FlowDocumentJson
{
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    public static string Serialize(FlowDocument document, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(document);

        var options = new JsonSerializerOptions(DefaultOptions)
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(document, options);
    }

    public static FlowDocument Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<FlowDocument>(json, DefaultOptions)
            ?? throw new InvalidOperationException("Flow document JSON is invalid.");
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
