using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Attempts to setup a component for localizing.
    /// </summary>
    [InitializeOnLoad]
    static class LocalizeComponent
    {
        static LocalizeComponent()
        {
            #if MODULE_AUDIO
            // Register known driven properties
            LocalizationPropertyDriver.UnityEventDrivenPropertiesLookup[(typeof(AudioSource), "set_clip")] = "m_audioClip";
            #endif
        }

        #if MODULE_AUDIO
        [MenuItem("CONTEXT/AudioSource/Localize")]
        static void LocalizeAudioSource(MenuCommand command)
        {
            var target = command.context as AudioSource;
            SetupForLocalization(target);
        }

        public static MonoBehaviour SetupForLocalization(AudioSource target)
        {
            var comp = Undo.AddComponent(target.gameObject, typeof(LocalizeAudioClipEvent)) as LocalizeAudioClipEvent;
            var setTextureMethod = target.GetType().GetProperty("clip").GetSetMethod();
            var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<AudioClip>), target, setTextureMethod) as UnityAction<AudioClip>;

            // TODO: Find any entry that is using the assigned clip
            Events.UnityEventTools.AddPersistentListener(comp.OnUpdateAsset, methodDelegate);
            Events.UnityEventTools.AddVoidPersistentListener(comp.OnUpdateAsset, target.Play);
            comp.OnUpdateAsset.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            comp.OnUpdateAsset.SetPersistentListenerState(1, UnityEventCallState.EditorAndRuntime);
            return comp;
        }

        #endif
    }
}
