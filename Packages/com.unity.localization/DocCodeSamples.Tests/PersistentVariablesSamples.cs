using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using Random = UnityEngine.Random;

public class GlobalVariablesSamples
{
    static void UpdateValue()
    {
        #region value-change-example
        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();
        var myFloat = source["global"]["my-float"] as FloatVariable;

        // This will trigger an update
        myFloat.Value = 123;
        #endregion
    }

    void AddGlobalVariable()
    {
        #region add-global-variable

        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();

        var globalVariables = source["globals"];

        var intVariable = new IntVariable { Value = 123 };

        // This can be accessed from a Smart String with the following syntax: {globals.my-int}
        globalVariables.Add("my-int", intVariable);
        #endregion
    }

    void TryGetAddGlobalVariable()
    {
        #region try-get-add-global-variable

        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();

        // If a group called "globals" does not exist then add one.
        if (!source.TryGetValue("globals", out var globalVariables))
        {
            globalVariables = ScriptableObject.CreateInstance<VariablesGroupAsset>();
            source.Add("globals", globalVariables);
        }

        var floatVariable = new FloatVariable { Value = 1.23f };

        // This can be accessed from a Smart String with the following syntax: {globals.my-float}
        globalVariables.Add("my-float", floatVariable);
        #endregion
    }
}

#region update-scope-example
public class RandomPlayerStats : MonoBehaviour
{
    public string[] stats = new[] { "vitality", "endurance", "strength", "dexterity", "intelligence" };

    public void RandomStats()
    {
        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();
        var nestedGroup = source["global-sample"]["player"] as NestedVariablesGroup;

        // An UpdateScope or using BeginUpdating and EndUpdating can be used to combine multiple changes into a single Update.
        // This prevents unnecessary string refreshes when updating multiple Global Variables.
        using (PersistentVariablesSource.UpdateScope())
        {
            foreach (var name in stats)
            {
                var variable = nestedGroup.Value[name] as IntVariable;
                variable.Value = Random.Range(0, 10);
            }
        }
    }
}
#endregion

#region date-time-example
/// <summary>
/// This is an example of a Global Variable that can return the current time.
/// </summary>
[DisplayName("Current Date Time")]
public class CurrentTime : IVariable
{
    public object GetSourceValue(ISelectorInfo _) => DateTime.Now;
}
#endregion

#region custom-date-time-example

[DisplayName("Date Time")]
[Serializable]
public class DateTimeVariable : IVariable
{
    [Range(1900, 2050)] public int year;
    [Range(0, 12)] public int month;
    [Range(0, 31)] public int day;
    [Range(0, 24)] public int hour;
    [Range(0, 60)] public int min;
    [Range(0, 60)] public int sec;

    public object GetSourceValue(ISelectorInfo _)
    {
        try
        {
            return new DateTime(year, month, day, hour, min, sec);
        }
        catch
        {
            // Ignore issues about incorrect values.
        }
        return new DateTime();
    }
}
#endregion

#region custom-list-loc-strings

[Serializable]
public class LocalizedStringList : IVariable
{
    public List<LocalizedString> localizeds = new List<LocalizedString>();

    public object GetSourceValue(ISelectorInfo selector)
    {
        return localizeds.Select(l => l.GetLocalizedString()).ToList();
    }
}
#endregion

#region custom-value-changed-example
[Serializable]
public class MyVariable : IVariable, IVariableValueChanged
{
    [SerializeField]
    string m_Value;

    public string Value
    {
        get => m_Value;
        set
        {
            if (m_Value == value)
                return;

            m_Value = value;
            ValueChanged?.Invoke(this);
        }
    }

    public event Action<IVariable> ValueChanged;

    public object GetSourceValue(ISelectorInfo _) => Value;
}
#endregion

#region custom-group-example
struct ReturnValue : IVariable
{
    public object SourceValue { get; set; }

    public object GetSourceValue(ISelectorInfo _) => SourceValue;
}

/// <summary>
/// This example shows how a nested group can be used to return custom data without the need for Reflection.
/// </summary>
[DisplayName("Weapon Damage")]
[Serializable]
public class WeaponDamageGroup : IVariableGroup, IVariable
{
    public object GetSourceValue(ISelectorInfo _) => this;

    public bool TryGetValue(string key, out IVariable value)
    {
        switch (key)
        {
            case "sword":
                value = new ReturnValue { SourceValue = 6 };
                return true;

            case "mace":
                value = new ReturnValue { SourceValue = 5 };
                return true;

            case "axe":
                value = new ReturnValue { SourceValue = 8 };
                return true;

            case "dagger":
                value = new ReturnValue { SourceValue = 2 };
                return true;
        }

        value = null;
        return false;
    }
}
#endregion

// In its own namespace so we dont conflict with the version in Samples
namespace Sample1
{
    #region metadata-variable1
    [Metadata(AllowedTypes = MetadataType.StringTableEntry)]
    [Serializable]
    public class ItemGender : IMetadata
    {
        public enum Gender
        {
            None,
            Female,
            Male
        }

        public Gender gender = Gender.None;
    }
    #endregion
}

// In its own namespace so we dont conflict with the version in Samples
namespace Sample2
{
    #region metadata-variable2
    [Metadata(AllowedTypes = MetadataType.StringTableEntry)]
    [Serializable]
    public class ItemGender : IMetadata, IMetadataVariable
    {
        public enum Gender
        {
            None,
            Female,
            Male
        }

        public Gender gender = Gender.None;

        /// <summary>
        /// The name used to identify this metadata.
        /// </summary>
        public string VariableName => "gender";

        public object GetSourceValue(ISelectorInfo _) => gender;
    }
    #endregion
}

#region xml-example
[DisplayName("XML Text")]
public class XmlElement : IVariable
{
    public string xmlText;

    public object GetSourceValue(ISelectorInfo selector)
    {
        try
        {
            if (!string.IsNullOrEmpty(xmlText))
            {
                return XElement.Parse(xmlText);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return null;
    }
}
#endregion
