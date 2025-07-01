using System.Reflection;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.Authoring {
    //HACK: This is big hack but it is fastest way to refresh Unity lod data cache.
    // For more info please see SelectLodEnabled. We cannot easily edit MovementGraceFixed16 and lod bias changes
    // are even more hacky.
    public class ForceRefreshLods : MonoBehaviour {
        static readonly FieldInfo _forceLowLodField = typeof(EntitiesGraphicsSystem)
            .GetField("m_ForceLowLOD", BindingFlags.NonPublic | BindingFlags.Instance);

        // Called also from Animator/Timeline
        [Button]
        public void ForceRefresh() {
            var world = World.DefaultGameObjectInjectionWorld;
            EntitiesGraphicsSystem graphicsSystem = world.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var forceLowLod = (NativeList<byte>)_forceLowLodField.GetValue(graphicsSystem);
            if (!forceLowLod.IsCreated) {
                return;
            }
            new MemsetNativeArray<byte> {
                Source = forceLowLod.AsArray(),
                Value = (byte)(forceLowLod[0] == 0 ? 1 : 0),
            }.Run(forceLowLod.Length);
        }
    }
}
