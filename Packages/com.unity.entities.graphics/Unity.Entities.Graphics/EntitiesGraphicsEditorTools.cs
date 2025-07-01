using UnityEditor;

namespace Unity.Rendering
{
    internal static class EntitiesGraphicsEditorTools
    {
        internal struct EntitiesGraphicsDebugSettings
        {
            // error CS0649: Field is never assigned to, and will always have its default value 0
#pragma warning disable CS0649
            public bool RecreateAllBatches;
            public bool ForceInstanceDataUpload;
#pragma warning restore CS0649
        }

#if UNITY_EDITOR
        [MenuItem("Edit/Rendering/Entities Graphics/Reupload all instance data")]
        internal static void ReuploadAllInstanceData()
        {
            s_EntitiesGraphicsDebugSettings.ForceInstanceDataUpload = true;
        }

        [MenuItem("Edit/Rendering/Entities Graphics/Recreate all batches")]
        internal static void RecreateAllBatches()
        {
            s_EntitiesGraphicsDebugSettings.RecreateAllBatches = true;
        }

        internal static void EndFrame()
        {
            s_EntitiesGraphicsDebugSettings = default;
        }

        private static EntitiesGraphicsDebugSettings s_EntitiesGraphicsDebugSettings;
        internal static EntitiesGraphicsDebugSettings DebugSettings => s_EntitiesGraphicsDebugSettings;

#else
        internal static void EndFrame()
        {
        }

        internal static EntitiesGraphicsDebugSettings DebugSettings => default;
#endif
    }
}
