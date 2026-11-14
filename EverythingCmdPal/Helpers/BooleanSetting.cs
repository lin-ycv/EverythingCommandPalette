using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace EverythingCmdPal.Helpers;

// Preserve the toolkit's card and on-disk format, but accept both representations
// of a submitted checkbox. The published SDK accepts string values only.
internal sealed class BooleanSetting(string key, string label, string description, bool value)
    : Setting<bool>(key, label, description, value)
{
    public override Dictionary<string, object> ToDictionary() => new ToggleSetting(Key, Label, Description, Value)
    {
        IsRequired = IsRequired,
        ErrorMessage = ErrorMessage,
    }.ToDictionary();

    public override void Update(JsonObject payload)
    {
        if (payload[Key] is not JsonValue value) return;
        if (value.TryGetValue<bool>(out bool flag) ||
            (value.TryGetValue<string>(out string? text) && bool.TryParse(text, out flag)))
            Value = flag;
        else
            throw new FormatException($"Invalid checkbox value for {Key}.");
    }

    public override string ToState() => $"\"{Key}\": \"{(Value ? "true" : "false")}\"";
}
