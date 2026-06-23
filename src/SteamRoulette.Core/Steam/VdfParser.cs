using System.Text;

namespace SteamRoulette.Core.Steam;

/// <summary>
/// A node in a parsed Valve Data Format (VDF/KeyValues) document: either a leaf with a
/// string <see cref="Value"/>, or an object with <see cref="Children"/>.
/// </summary>
public sealed class VdfNode
{
    public string? Value { get; init; }
    public Dictionary<string, VdfNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsLeaf => Value is not null;

    /// <summary>Child by key, or null if absent. Case-insensitive.</summary>
    public VdfNode? this[string key] => Children.TryGetValue(key, out var n) ? n : null;
}

/// <summary>
/// Minimal recursive-descent parser for the text VDF format Steam uses for
/// libraryfolders.vdf and appmanifest_*.acf. Dependency-free; handles quoted and
/// unquoted tokens, nested objects, escape sequences, and // line comments.
/// </summary>
public static class VdfParser
{
    public static VdfNode Parse(string text)
    {
        int pos = 0;
        var root = new VdfNode();
        ParseObject(root, text, ref pos, isRoot: true);
        return root;
    }

    private static void ParseObject(VdfNode obj, string s, ref int pos, bool isRoot)
    {
        while (true)
        {
            var key = NextToken(s, ref pos);
            if (key is null) return;          // EOF
            if (key == "}")                    // close of the current object
            {
                if (isRoot) continue;          // stray brace at top level, ignore
                return;
            }

            var val = NextToken(s, ref pos);
            if (val is null)
            {
                obj.Children[key] = new VdfNode { Value = "" };
                return;
            }

            if (val == "{")
            {
                var child = new VdfNode();
                ParseObject(child, s, ref pos, isRoot: false);
                obj.Children[key] = child;
            }
            else if (val == "}")
            {
                obj.Children[key] = new VdfNode { Value = "" };
                if (!isRoot) return;
            }
            else
            {
                obj.Children[key] = new VdfNode { Value = val };
            }
        }
    }

    /// <summary>Next token: a string, or "{"/"}" as structural markers, or null at EOF.</summary>
    private static string? NextToken(string s, ref int pos)
    {
        SkipTrivia(s, ref pos);
        if (pos >= s.Length) return null;

        char c = s[pos];
        if (c == '{' || c == '}')
        {
            pos++;
            return c.ToString();
        }

        if (c == '"')
        {
            pos++;
            var sb = new StringBuilder();
            while (pos < s.Length && s[pos] != '"')
            {
                if (s[pos] == '\\' && pos + 1 < s.Length)
                {
                    pos++;
                    sb.Append(s[pos] switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '"' => '"',
                        var other => other,
                    });
                }
                else
                {
                    sb.Append(s[pos]);
                }
                pos++;
            }
            pos++; // consume closing quote
            return sb.ToString();
        }

        // Unquoted token: read until whitespace or a structural character.
        int start = pos;
        while (pos < s.Length && !char.IsWhiteSpace(s[pos]) &&
               s[pos] is not ('{' or '}' or '"'))
        {
            pos++;
        }
        return s[start..pos];
    }

    private static void SkipTrivia(string s, ref int pos)
    {
        while (pos < s.Length)
        {
            if (char.IsWhiteSpace(s[pos]))
            {
                pos++;
                continue;
            }
            if (s[pos] == '/' && pos + 1 < s.Length && s[pos + 1] == '/')
            {
                while (pos < s.Length && s[pos] != '\n') pos++;
                continue;
            }
            break;
        }
    }
}
