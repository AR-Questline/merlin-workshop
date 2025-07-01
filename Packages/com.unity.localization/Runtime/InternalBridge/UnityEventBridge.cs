using System.Reflection;
using UnityEngine.Events;

namespace UnityEngine.Localization.Bridge
{
    internal static class UnityEventBridge
    {
        static readonly FieldInfo k_PersistenCallGroup = typeof(UnityEventBase).GetField("m_PersistentCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static UnityEventCallState GetPersistentListenerState(this UnityEventBase unityEvent, int index)
        {
            var group = (PersistentCallGroup)k_PersistenCallGroup.GetValue(unityEvent);
            return group.GetListener(index).callState;
        }
    }
}
