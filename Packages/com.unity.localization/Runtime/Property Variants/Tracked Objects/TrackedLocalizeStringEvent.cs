#if UNITY_EDITOR && ENABLE_PROPERTY_VARIANTS

using System;
using UnityEngine.Localization.Components;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    [CustomTrackedObject(typeof(LocalizeStringEvent), false)]
    class TrackedLocalizeStringEvent : TrackedObject
    {
        public override bool CanTrackProperty(string _) => false;

        public override AsyncOperationHandle ApplyLocale(Locale variantLocale, Locale defaultLocale)
        {
            throw new NotSupportedException("Tracking LocalizeStringEvent is not supported.");
        }
    }
}

#endif
