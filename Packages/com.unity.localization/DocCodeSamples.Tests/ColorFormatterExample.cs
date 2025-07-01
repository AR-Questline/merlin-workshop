using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

[DisplayName("Color Formatter")]
public class ColorFormatter : FormatterBase
{
    public override string[] DefaultNames => new string[] { "color" };

    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        if (formattingInfo.CurrentValue is Color color)
        {
            formattingInfo.Write(ColorUtility.ToHtmlStringRGB(color));
            return true;
        }
        return false;
    }
}
