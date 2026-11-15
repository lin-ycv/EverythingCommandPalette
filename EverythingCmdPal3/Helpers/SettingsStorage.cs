using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace EverythingCmdPal3.Helpers;

internal static class SettingsStorage
{
    // The toolkit persists toggle values as strings. Accept native JSON booleans
    // too, including files edited by users and different Adaptive Card renderers.
    internal static string Normalize(string json)
    {
        var settings = JsonNode.Parse(json)?.AsObject()
            ?? throw new FormatException("Everything settings must be a JSON object.");
        foreach (var entry in settings.ToArray())
        {
            if (entry.Value is JsonValue value && value.TryGetValue<bool>(out bool flag))
                settings[entry.Key] = flag ? "true" : "false";
            else if (entry.Value is JsonValue number && number.GetValueKind() == System.Text.Json.JsonValueKind.Number)
                settings[entry.Key] = number.ToJsonString();
        }
        return settings.ToJsonString();
    }

    internal static void Save(string path, string values)
    {
        var saved = File.Exists(path)
            ? JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            : new JsonObject();
        if (saved is null)
            throw new FormatException("Everything settings must be a JSON object.");
        foreach (var entry in JsonNode.Parse(values)!.AsObject())
            saved[entry.Key] = entry.Value?.DeepClone();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, saved.ToJsonString(new() { WriteIndented = true }));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}

