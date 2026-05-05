namespace ProjectHub.Domain.Models;

/// <summary>
/// A fixed palette of 16 light hex colours used to tint a client's tabs (and
/// every project / sub-tab nested under it). New clients are auto-assigned
/// the next colour in rotation; the value is editable through the colour
/// picker on the client view.
/// </summary>
public static class ClientColours
{
    /// <summary>The default colour applied when none has been picked yet.</summary>
    public const string Default = "#E2E8F0";

    /// <summary>16 light, visually distinct hex colours.</summary>
    public static readonly IReadOnlyList<string> Palette = new[]
    {
        "#FED7D7", // rose
        "#FEEBC8", // peach
        "#FEFCBF", // butter
        "#C6F6D5", // mint
        "#B2F5EA", // aqua
        "#BEE3F8", // sky
        "#C3DAFE", // periwinkle
        "#D6BCFA", // lilac
        "#FBB6CE", // blossom
        "#FAD2E1", // sakura
        "#F6E0B5", // sand
        "#D9F99D", // pistachio
        "#A7F3D0", // jade
        "#A5F3FC", // ice
        "#BAE6FD", // cornflower
        "#E9D5FF"  // wisteria
    };

    /// <summary>
    /// Picks the next colour in <see cref="Palette"/> for a newly created
    /// client, cycling around once exhausted. The choice is deterministic
    /// given the existing client count.
    /// </summary>
    public static string AutoAssign(int existingClientCount)
    {
        var index = ((existingClientCount % Palette.Count) + Palette.Count) % Palette.Count;
        return Palette[index];
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="colour"/> is a
    /// well-formed hex string of the form <c>#RRGGBB</c>.
    /// </summary>
    public static bool IsValid(string? colour)
    {
        if (string.IsNullOrEmpty(colour) || colour.Length != 7 || colour[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < 7; i++)
        {
            if (!Uri.IsHexDigit(colour[i]))
            {
                return false;
            }
        }

        return true;
    }
}
