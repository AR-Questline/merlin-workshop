#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    /// <summary>
    /// Tracks and applies variant changes to a [MeshFilter](https://docs.unity3d.com/ScriptReference/MeshFilter.html).
    /// </summary>
    [Serializable]
    [DisplayName("Mesh Filter")]
    [CustomTrackedObject(typeof(MeshFilter), false)]
    public class TrackedMeshFilter : TrackedObject
    {
        const string k_MeshProperty = "m_Mesh";

        AsyncOperationHandle<Mesh> m_CurrentOperation;

        /// <inheritdoc/>
        public override bool CanTrackProperty(string propertyPath) => propertyPath == k_MeshProperty;

        /// <inheritdoc/>
        public override AsyncOperationHandle ApplyLocale(Locale variantLocale, Locale defaultLocale)
        {
            if (TrackedProperties.Count == 0)
                return default;

            if (m_CurrentOperation.IsValid())
            {
                if (!m_CurrentOperation.IsDone)
                    m_CurrentOperation.Completed -= MeshOperationCompleted;
                AddressablesInterface.SafeRelease(m_CurrentOperation);
                m_CurrentOperation = default;
            }

            Debug.Assert(TrackedProperties.Count == 1, "Expected only 1 property to be tracked for a MeshFilter", Target);

            var property = TrackedProperties[0];

            #if UNITY_EDITOR
            VariantsPropertyDriver.RegisterProperty(Target, property.PropertyPath);
            #endif

            Debug.AssertFormat(property.PropertyPath == k_MeshProperty, Target, "Expected tracked property {0} but it was {1}.", k_MeshProperty, property.PropertyPath);

            if (property is UnityObjectProperty objectProperty)
            {
                var fallbackIdentifier = defaultLocale != null ? defaultLocale.Identifier : default;
                if (objectProperty.GetValue(variantLocale.Identifier, fallbackIdentifier, out var meshObject))
                {
                    SetMesh(meshObject as Mesh);
                }
            }
            else if (property is LocalizedAssetProperty localizedAssetProperty && !localizedAssetProperty.LocalizedObject.IsEmpty)
            {
                m_CurrentOperation = localizedAssetProperty.LocalizedObject.LoadAssetAsync<Mesh>();
                 
                if (m_CurrentOperation.IsDone)
                {
                    MeshOperationCompleted(m_CurrentOperation);
                }
                #if !UNITY_WEBGL // WebGL does not support WaitForCompletion
                else if (localizedAssetProperty.LocalizedObject.ForceSynchronous)
                {
                    var result = m_CurrentOperation.WaitForCompletion();
                    MeshOperationCompleted(m_CurrentOperation);
                }
                #endif
                else
                {
                    m_CurrentOperation.Completed += MeshOperationCompleted;
                    return m_CurrentOperation;
                }
            }

            return default;
        }

        void MeshOperationCompleted(AsyncOperationHandle<Mesh> assetOp) => SetMesh(assetOp.Result);
        void SetMesh(Mesh mesh) => ((MeshFilter)Target).sharedMesh = mesh;
    }
}

#endif
