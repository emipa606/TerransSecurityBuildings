using UnityEngine;
using Verse;

namespace TerrenSecurity;

[StaticConstructorOnStartup]
public static class SettingGUI
{
    public static void Settings_IntegerBox(this Listing_Standard lister, string text, ref int value, float labelLength,
        float padding, int min = int.MinValue, int max = int.MaxValue)
    {
        lister.Gap();
        var rect = lister.GetRect(Text.LineHeight);
        var rect2 = new Rect(rect.x, rect.y, labelLength, rect.height);
        var rect3 = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding, rect.height);
        var color = GUI.color;
        Widgets.Label(rect2, text);
        var alignment = Text.CurTextFieldStyle.alignment;
        Text.CurTextFieldStyle.alignment = TextAnchor.MiddleLeft;
        var buffer = value.ToString();
        Widgets.TextFieldNumeric(rect3, ref value, ref buffer, min, max);
        Text.CurTextFieldStyle.alignment = alignment;
        GUI.color = color;
    }
}