#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.Localization.Bridge;
using UnityEngine.Pool;
#endif

namespace UnityEngine.Localization.Components
{
    /// <summary>
    /// Provides Editor support for Localization.
    /// </summary>
    [ExecuteAlways]
    public class LocalizedMonoBehaviour : MonoBehaviour
    {
        #if UNITY_EDITOR

        HashSet<(Object, string)> m_Tracked = new HashSet<(Object, string)>();

        protected void Editor_RegisterKnownDrivenProperties(UnityEventBase unityEvent)
        {
            var previousTracked = m_Tracked;
            var tracked = HashSetPool<(Object, string)>.Get();

            for (int i = 0; i < unityEvent.GetPersistentEventCount(); ++i)
            {
                var target = unityEvent.GetPersistentTarget(i);

                if (target == null || unityEvent.GetPersistentListenerState(i) != UnityEventCallState.EditorAndRuntime)
                    continue;

                var methodName = unityEvent.GetPersistentMethodName(i);
                if (LocalizationPropertyDriver.UnityEventDrivenPropertiesLookup.TryGetValue((target.GetType(), methodName), out var foundPath))
                {
                    LocalizationPropertyDriver.RegisterProperty(target, foundPath);
                    var key = (target, foundPath);
                    tracked.Add(key);
                    previousTracked.Remove(key);
                }
            }

            // Unregister properties we no longer track
            if (previousTracked.Count > 0)
            {
                foreach (var t in previousTracked)
                {
                    LocalizationPropertyDriver.UnregisterProperty(t.Item1, t.Item2);
                }
            }

            m_Tracked = tracked;
            HashSetPool<(Object, string)>.Release(previousTracked);
        }

        protected void Editor_UnregisterKnownDrivenProperties(UnityEventBase unityEvent)
        {
            m_Tracked.Clear();
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); ++i)
            {
                var target = unityEvent.GetPersistentTarget(i);

                if (target == null)
                    continue;

                var methodName = unityEvent.GetPersistentMethodName(i);
                if (LocalizationPropertyDriver.UnityEventDrivenPropertiesLookup.TryGetValue((target.GetType(), methodName), out var foundPath))
                {
                    LocalizationPropertyDriver.UnregisterProperty(target, foundPath);
                }
            }
        }

        protected void Editor_RefreshEventObjects(UnityEventBase unityEvent)
        {
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); ++i)
            {
                var target = unityEvent.GetPersistentTarget(i);
                if (target == null || unityEvent.GetPersistentListenerState(i) != UnityEventCallState.EditorAndRuntime)
                    continue;

                RefreshObject(target);
            }
        }

        void RefreshObject(Object target)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            #if PACKAGE_UGUI
            if (target is UI.Graphic graphic)
            {
                // TODO: Can we avoid making the scene dirty?
                graphic.SetAllDirty();
            }
            else
            #endif
            {
                // Call OnValidate, this will often force a refresh of the component when not playing.
                var method = target.GetType().GetMethod("OnValidate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(target, null);
            }
        }

        #endif
    }
}
