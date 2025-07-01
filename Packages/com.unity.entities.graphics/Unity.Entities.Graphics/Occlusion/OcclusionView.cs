using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Unity.Rendering.Occlusion
{
    
    /// <summary>
    /// Represents occlusion view settings.
    /// </summary>
    public struct OcclusionViewSettings
    {
        /// <summary>
        /// Indicates whether to process occlusion culling for the occlusion view.
        /// </summary>
        public bool enabled;
        /// <summary>
        /// The width of the occlusion buffer.
        /// </summary>
        public uint width;
        /// <summary>
        /// The height of the occlusion buffer.
        /// </summary>
        public uint height;
    }

    /// <summary>
    /// Explicitly specifies which frustum views are occlusion views and configures occlusion view settings.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class OcclusionView : MonoBehaviour
    {
        /// <summary>
        /// Indicates whether to process occlusion culling for the attached frustum view.
        /// </summary>
        public bool OcclusionEnabled = true;

        /// <summary>
        /// The width of the occlusion buffer.
        /// </summary>
        public uint OcclusionBufferWidth = DefaultBufferSize;

        /// <summary>
        /// The height of the occlusion buffer.
        /// </summary>
        public uint OcclusionBufferHeight = DefaultBufferSize;

        /// <summary>
        /// The default value for the occlusion buffer height and width.
        /// </summary>
        public static readonly uint DefaultBufferSize = 512;

#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

        void Update()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return;

            var entitiesGraphicsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

            if (entitiesGraphicsSystem != null)
            {
                var occlusion = entitiesGraphicsSystem.OcclusionCulling;
                if (occlusion != null)
                {
                    occlusion.UpdateSettings(this);
                }
            }
        }

        public void OnValidate()
        {
            Update();
#if UNITY_EDITOR
            OcclusionBrowseWindow.Refresh();
#endif
        }

        private void OnDestroy()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return;

            var entitiesGraphicsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

            if (entitiesGraphicsSystem != null)
            {
                var occlusion = entitiesGraphicsSystem.OcclusionCulling;
                if (occlusion != null)
                {
                    occlusion.UpdateSettings(this);
                }
            }
        }
#endif
    }
}
