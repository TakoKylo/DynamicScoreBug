using UnityEngine;

public sealed partial class Scoreboard_ClientMod
{
    private static UnityEngine.Color ParseHexColor(string hex, UnityEngine.Color fallback)
    {
        if (string.IsNullOrEmpty(hex))
            return fallback;

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (UnityEngine.ColorUtility.TryParseHtmlString("#" + hex, out UnityEngine.Color color))
            return color;

        return fallback;
    }
}
