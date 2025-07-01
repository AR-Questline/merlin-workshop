using System;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace UnityEngine.Localization.Samples
{
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
}
