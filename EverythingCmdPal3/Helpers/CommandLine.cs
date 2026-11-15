using System.Text;

namespace EverythingCmdPal3.Helpers;

internal static class CommandLine
{
    // Everything 1.5 parses its own arguments. -s* consumes the remaining text
    // literally; Windows argv quoting would change quoted paths in the query.
    internal static string EverythingSearch(string query, string instance) =>
        $"-instance \"{instance.Replace("\"", "\"\"\"")}\" -s* {query}";

    // Windows command-line quoting, including backslashes before quotes/the end.
    internal static string QuoteArgument(string value)
    {
        var result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { ++slashes; continue; }
            result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
            result.Append(c);
            slashes = 0;
        }
        result.Append('\\', slashes * 2);
        return result.Append('"').ToString();
    }
}
