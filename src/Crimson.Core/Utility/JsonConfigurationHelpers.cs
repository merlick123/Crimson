using System.Text.Json;

namespace Crimson.Core.Utility;

internal static class JsonConfigurationHelpers
{
    public static string ResolveOutputRoot(JsonElement configuration, string defaultOutputRoot) =>
        GetOptionalString(configuration, "output") ?? defaultOutputRoot;

    public static string? GetOptionalString(JsonElement configuration, string propertyName)
    {
        if (configuration.ValueKind != JsonValueKind.Object ||
            !configuration.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.GetString();
    }

    public static JsonElement? GetOptionalObject(JsonElement configuration, string propertyName)
    {
        if (configuration.ValueKind != JsonValueKind.Object ||
            !configuration.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return property;
    }
}
