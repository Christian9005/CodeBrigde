using System.Text.Json;

namespace CodeBridge.Flow.Execution;

internal static class FlowValueConverter
{
    public static T ConvertTo<T>(object? value, string name)
    {
        if (value is null)
            throw new InvalidOperationException($"Value '{name}' is required.");

        if (value is T typed)
            return typed;

        if (value is JsonElement json)
            return ConvertJsonElement<T>(json, name);

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType == typeof(string))
            return (T)(object)value.ToString()!;

        if (targetType == typeof(bool) && value is int intBool)
            return (T)(object)(intBool != 0);

        return (T)Convert.ChangeType(value, targetType);
    }

    private static T ConvertJsonElement<T>(JsonElement json, string name)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType == typeof(string))
            return (T)(object)(json.GetString() ?? string.Empty);

        if (targetType == typeof(bool))
            return (T)(object)json.GetBoolean();

        if (targetType == typeof(int))
            return (T)(object)json.GetInt32();

        if (targetType == typeof(double))
            return (T)(object)json.GetDouble();

        throw new InvalidOperationException($"Value '{name}' cannot be converted to {targetType.Name}.");
    }
}

