#if PACKAGE_TMP || (UNITY_2023_2_OR_NEWER && PACKAGE_UGUI)

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace UnityEditor.Localization.Plugins.TMPro
{
    [InitializeOnLoad]
    internal static class LocalizeComponent_TMPro
    {
        static LocalizeComponent_TMPro()
        {
            // Register known driven properties
            LocalizationPropertyDriver.UnityEventDrivenPropertiesLookup[(typeof(TextMeshProUGUI), "set_text")] = "m_text";
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/Localize")]
        static void LocalizeTMProText(MenuCommand command)
        {
            var target = command.context as TextMeshProUGUI;
            SetupForLocalization(target);
        }

        public static MonoBehaviour SetupForLocalization(TextMeshProUGUI target)
        {
            var comp = Undo.AddComponent(target.gameObject, typeof(LocalizeStringEvent)) as LocalizeStringEvent;
            var setStringMethod = target.GetType().GetProperty("text").GetSetMethod();
            var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, setStringMethod) as UnityAction<string>;
            Events.UnityEventTools.AddPersistentListener(comp.OnUpdateString, methodDelegate);
            comp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            return comp;
        }
    }
}

#endif
