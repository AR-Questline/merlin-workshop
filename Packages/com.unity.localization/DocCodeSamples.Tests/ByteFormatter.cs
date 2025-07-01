using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

[DisplayName("Base 2 Byte Formatter")]
public class ByteFormatter : FormatterBase
{
    public override string[] DefaultNames => new string[] { "byte" };

    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        if (formattingInfo.CurrentValue is long bytes)
        {
            // We are performing a Base 2 conversion here. 1024 bytes = 1 KB
            if (bytes < 512)
            {
                formattingInfo.Write($"{bytes} B");
                return true;
            }

            if (bytes < 512 * 1024)
            {
                var kb = bytes / 1024.0f;
                formattingInfo.Write($"{kb.ToString("0.00")} KB");
                return true;
            }

            bytes /= 1024;
            if (bytes < 512 * 1024)
            {
                var mb = bytes / 1024.0f;
                formattingInfo.Write($"{mb.ToString("0.00")} MB");
                return true;
            }

            bytes /= 1024;
            var gb = bytes / 1024.0f;
            formattingInfo.Write($"{gb.ToString("0.00")} GB");
            return true;
        }

        return false;
    }
}
