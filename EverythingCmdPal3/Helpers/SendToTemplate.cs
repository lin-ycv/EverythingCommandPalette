using System;

namespace EverythingCmdPal3.Helpers;

internal static class SendToTemplate
{
    internal static bool TryParse(string template, out string executable, out string arguments)
    {
        int separator = template.IndexOf(',');
        executable = separator < 0 ? string.Empty : template[..separator].Trim();
        arguments = separator < 0 ? string.Empty : template[(separator + 1)..];
        return executable.Length > 0;
    }
}

