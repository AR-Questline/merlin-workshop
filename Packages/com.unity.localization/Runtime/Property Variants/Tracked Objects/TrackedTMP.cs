#if (PACKAGE_TMP || (UNITY_2023_2_OR_NEWER && PACKAGE_UGUI)) && ENABLE_PROPERTY_VARIANTS

using System;
using TMPro;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    /// <summary>
    /// Uses JSON to apply variant data to <see cref="TMP_Dropdown"/>.
    /// Calls <see cref="TMP_Dropdown.RefreshShownValue"/> on the target object after applying changes.
    /// </summary>
    /// <example>
    /// This shows how to configure a <see cref="GameObjectLocalizer"/> to apply changes to a [TMP_Dropdown](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest/index.html?subfolder=/api/TMPro.TMP_Dropdown.html)
    /// for the options values.
    /// <code source="../../../DocCodeSamples.Tests/GameObjectLocalizerSamples.cs" region="setup-tmp-dropdown"/>
    /// </example>
    [Serializable]
    [DisplayName("TMP Dropdown")]
    [CustomTrackedObject(typeof(TMP_Dropdown), true)]
    public class TrackedTmpDropdown : JsonSerializerTrackedObject
    {
        protected override void PostApplyTrackedProperties()
        {
            ((TMP_Dropdown)Target).RefreshShownValue();
            base.PostApplyTrackedProperties();
        }
    }
}

#endif
