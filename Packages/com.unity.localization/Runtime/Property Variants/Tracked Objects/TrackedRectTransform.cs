#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    /// <summary>
    /// Tracks and applies variant changes to a [RectTransform](https://docs.unity3d.com/ScriptReference/RectTransform.html).
    /// </summary>
    /// <example>
    /// This shows how to configure a <see cref="GameObjectLocalizer"/> to apply changes to a RectTransform for the x, y and width properties.
    /// This can be useful when you need to make adjustments due to changes in text length for a particular <see cref="Locale"/>.
    /// <code source="../../../DocCodeSamples.Tests/GameObjectLocalizerSamples.cs" region="setup-rect-transform"/>
    /// </example>
    [Serializable]
    [DisplayName("Rect Transform")]
    [CustomTrackedObject(typeof(RectTransform), false)]
    public class TrackedRectTransform : TrackedTransform
    {
        Vector3 m_AnchorPosToApply;
        Vector2 m_AnchorMinToApply;
        Vector2 m_AnchorMaxToApply;
        Vector2 m_PivotToApply;
        Vector2 m_SizeDeltaToApply;

        protected override void AddPropertyHandlers(Dictionary<string, Action<float>> handlers)
        {
            base.AddPropertyHandlers(handlers);

            handlers["m_AnchoredPosition.x"] = val => m_AnchorPosToApply.x = val;
            handlers["m_AnchoredPosition.y"] = val => m_AnchorPosToApply.y = val;
            handlers["m_AnchoredPosition.z"] = val => m_AnchorPosToApply.z = val;
            handlers["m_AnchorMin.x"] = val => m_AnchorMinToApply.x = val;
            handlers["m_AnchorMin.y"] = val => m_AnchorMinToApply.y = val;
            handlers["m_AnchorMax.x"] = val => m_AnchorMaxToApply.x = val;
            handlers["m_AnchorMax.y"] = val => m_AnchorMaxToApply.y = val;
            handlers["m_SizeDelta.x"] = val => m_SizeDeltaToApply.x = val;
            handlers["m_SizeDelta.y"] = val => m_SizeDeltaToApply.y = val;
            handlers["m_Pivot.x"] = val => m_PivotToApply.x = val;
            handlers["m_Pivot.y"] = val => m_PivotToApply.y = val;
        }

        public override AsyncOperationHandle ApplyLocale(Locale variantLocale, Locale defaultLocale)
        {
            var rectTransform = (RectTransform)Target;
            m_AnchorPosToApply = rectTransform.anchoredPosition3D;
            m_AnchorMinToApply = rectTransform.anchorMin;
            m_AnchorMaxToApply = rectTransform.anchorMax;
            m_PivotToApply = rectTransform.pivot;
            m_SizeDeltaToApply = rectTransform.sizeDelta;

            base.ApplyLocale(variantLocale, defaultLocale);

            rectTransform.anchoredPosition3D = m_AnchorPosToApply;
            rectTransform.anchorMin = m_AnchorMinToApply;
            rectTransform.anchorMax = m_AnchorMaxToApply;
            rectTransform.pivot = m_PivotToApply;
            rectTransform.sizeDelta = m_SizeDeltaToApply;

            return default;
        }
    }
}

#endif
