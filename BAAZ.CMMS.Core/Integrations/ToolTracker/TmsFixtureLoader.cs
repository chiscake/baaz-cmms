using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

internal static class TmsFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static T Load<T>(string fileName)
    {
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "Integrations", "ToolTracker", "Fixtures", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"TMS fixture not found: {path}", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize fixture {fileName}");
    }
}
