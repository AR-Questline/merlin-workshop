using UnityEngine;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

[System.Serializable]
public class RandomValueSource : ISource
{
    public int min = 1;
    public int max = 5;

    public string selector = "random";

    public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        if (selectorInfo.SelectorText != selector)
            return false;

        selectorInfo.Result = Random.Range(min, max);
        return true;
    }
}
