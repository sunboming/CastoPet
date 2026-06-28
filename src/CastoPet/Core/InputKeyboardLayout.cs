using System.Drawing;

namespace CastoPet.Core;

public static class InputKeyboardLayout
{
    public const float VisualWidth = 320;
    public const float VisualHeight = 420;

    private const float KeyWidth = 20;
    private const float KeyHeight = 18;
    private const float Gap = 4;

    private static readonly IReadOnlyDictionary<string, RectangleF> KeyBounds = BuildKeyBounds();

    public static IReadOnlyList<string> KeyIds { get; } = KeyBounds.Keys
        .OrderBy(key => KeyBounds[key].Y)
        .ThenBy(key => KeyBounds[key].X)
        .ToArray();

    public static bool TryGetKeyBounds(string key, out RectangleF bounds)
    {
        return KeyBounds.TryGetValue(key, out bounds);
    }

    private static IReadOnlyDictionary<string, RectangleF> BuildKeyBounds()
    {
        var keys = new Dictionary<string, RectangleF>(StringComparer.OrdinalIgnoreCase);

        AddRow(keys, new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Backspace" }, x: 26, y: 272);
        AddRow(keys, new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" }, x: 38, y: 296);
        AddRow(keys, new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L", "Enter" }, x: 50, y: 320);
        AddRow(keys, new[] { "Shift", "Z", "X", "C", "V", "B", "N", "M" }, x: 38, y: 344);

        keys["Ctrl"] = new RectangleF(50, 368, 28, KeyHeight);
        keys["Alt"] = new RectangleF(82, 368, 28, KeyHeight);
        keys["Space"] = new RectangleF(114, 368, 92, KeyHeight);
        keys["Left"] = new RectangleF(218, 368, 20, KeyHeight);
        keys["Down"] = new RectangleF(242, 368, 20, KeyHeight);
        keys["Up"] = new RectangleF(242, 344, 20, KeyHeight);
        keys["Right"] = new RectangleF(266, 368, 20, KeyHeight);
        keys["MouseLeft"] = new RectangleF(38, 248, 34, 16);
        keys["MouseRight"] = new RectangleF(76, 248, 34, 16);
        keys["MouseMiddle"] = new RectangleF(114, 248, 34, 16);

        return keys;
    }

    private static void AddRow(IDictionary<string, RectangleF> keys, IReadOnlyList<string> row, float x, float y)
    {
        var cursor = x;
        foreach (var key in row)
        {
            var width = key switch
            {
                "Backspace" => 42,
                "Enter" => 36,
                "Shift" => 42,
                _ => KeyWidth,
            };
            keys[key] = new RectangleF(cursor, y, width, KeyHeight);
            cursor += width + Gap;
        }
    }
}
