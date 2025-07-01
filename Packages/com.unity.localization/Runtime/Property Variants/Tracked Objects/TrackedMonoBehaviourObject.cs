#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;
using UnityEngine.Events;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    /// <summary>
    /// Uses JSON to apply variant data to target object.
    /// </summary>
    /// <example>
    /// This shows how to configure a <see cref="GameObjectLocalizer"/> to apply changes to a custom <see cref="MonoBehaviour"/> script.
    /// <code source="../../../DocCodeSamples.Tests/GameObjectLocalizerSamples.cs" region="user-script"/>
    /// </example>
    [Serializable]
    [CustomTrackedObject(typeof(MonoBehaviour), true)]
    public class TrackedMonoBehaviourObject : JsonSerializerTrackedObject
    {
        [SerializeField]
        UnityEvent m_Changed = new UnityEvent();

        public UnityEvent Changed => m_Changed;

        protected override void PostApplyTrackedProperties()
        {
            base.PostApplyTrackedProperties();
            m_Changed.Invoke();
        }
    }
}

#endif
