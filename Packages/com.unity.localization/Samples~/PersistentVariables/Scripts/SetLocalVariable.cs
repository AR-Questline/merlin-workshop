using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class SetLocalVariable : MonoBehaviour
{
    public LocalizeStringEvent localizedString;

    public void SetNestedStringEntry(string variableAndEntry)
    {
        var args = variableAndEntry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length != 2)
            return;

        if (localizedString.StringReference[args[0]] is LocalizedString nested)
        {
            nested.TableEntryReference = args[1];
        }
    }
}
