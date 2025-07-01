#if UNITY_2021_1_OR_NEWER

using UnityEditor.DeviceSimulation;
using UnityEditor.Localization.UI;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.DeviceSimulator
{
    class LocalizationDeviceSimulatorPlugin : DeviceSimulatorPlugin
    {
        public override string title => "Localization";

        public override VisualElement OnCreateUI()
        {
            return new ProjectLocalePopupField { label = "Locale" };
        }
    }
}

#endif
