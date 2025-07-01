#if PACKAGE_DEVICE_SIMULATOR

using Unity.DeviceSimulator;
using UnityEditor.Localization.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.DeviceSimulator
{
    /// <summary>
    /// Provides a popup menu in the Device Simulator window for changing the <see cref="LocalizationSettings.SelectedLocale"/>.
    /// </summary>
    class PackageLanguageMenu : VisualElement, IDeviceSimulatorExtension
    {
        public string extensionTitle => "Localization";

        public void OnExtendDeviceSimulator(VisualElement visualElement)
        {
            var menu = new ProjectLocalePopupField { label = "Locale" };
            visualElement.Add(menu);
        }
    }
}

#endif
