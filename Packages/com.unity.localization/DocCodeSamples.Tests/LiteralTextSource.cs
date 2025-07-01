using System;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

[Serializable]
public class LiteralTextSource : ISource
{
    public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        if (selectorInfo.SelectorText.Length < 3)
            return false;

        int len = selectorInfo.SelectorText.Length;
        if (selectorInfo.SelectorText[0] == '\"' && selectorInfo.SelectorText[len - 1] == '\"')
        {
            selectorInfo.Result = selectorInfo.SelectorText.Substring(1, len - 2);
            return true;
        }
        return false;
    }
}
