using System;

namespace Sirenix.OdinInspector
{
    [Flags]
    public enum PrefabKind
    {
        None = 0,
        InstanceInScene = 1,
        InstanceInPrefab = 2,
        Regular = 4,
        Variant = 8,
        NonPrefabInstance = 16, // 0x00000010
        PrefabInstance = InstanceInPrefab | InstanceInScene, // 0x00000003
        PrefabAsset = Variant | Regular, // 0x0000000C
        PrefabInstanceAndNonPrefabInstance = PrefabInstance | NonPrefabInstance, // 0x00000013
        All = PrefabInstanceAndNonPrefabInstance | PrefabAsset, // 0x0000001F
    }
}