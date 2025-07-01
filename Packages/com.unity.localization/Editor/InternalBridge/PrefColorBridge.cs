using UnityEngine;

namespace UnityEditor.Localization.Bridge
{
    // We have to InitializeOnLoad in order to add the preference or it wont be visible until we attempt to use the color in the inspector.
    [InitializeOnLoad]
    static class PrefColorBridge
    {
        static readonly PrefColor k_DrivenProperty;
        static readonly PrefColor k_VariantDrivenProperty;
        static readonly PrefColor k_VariantWithOverrideProperty;

        static PrefColorBridge()
        {
            // We need to InitializeOnLoad so that the color preference gets added and is shown in the Preferences/Colors window.
            k_DrivenProperty = new PrefColor("Localization/Driven Property", 0, 0.6037736f, 0, 0.7607843f, 0, 1, 0, 1);
            k_VariantDrivenProperty = new PrefColor("Localization/Variant Property", 0, 0.9886322f, 0, 0.7607843f, 0, 1, 0, 1);
            k_VariantWithOverrideProperty = new PrefColor("Localization/Variant Property(With Override)", 0, 0.6037736f, 0, 0.7607843f, 0, 1, 0, 1);
        }

        public static Color DrivenProperty => k_DrivenProperty.Color;
        public static Color VariantProperty => k_VariantDrivenProperty.Color;
        public static Color VariantWithOverrideProperty => k_VariantWithOverrideProperty.Color;
    }
}
