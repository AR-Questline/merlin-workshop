using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;

namespace Awaken.ECS.DrakeRenderer.Utilities {
    public struct MaterialsOverridePack {
        public UnsafeArray<MaterialOverrideData>.Span overrideDatas;
        public UnsafeArray<FixedString128Bytes>.Span materialKeys;
        public UnsafeArray<MaterialOverrideData>.Span defaultData;

        public MaterialsOverridePack(UnsafeArray<MaterialOverrideData>.Span overrideDatas, UnsafeArray<FixedString128Bytes>.Span materialKeys, UnsafeArray<MaterialOverrideData>.Span defaultData = default) {
            this.defaultData = defaultData;
            this.overrideDatas = overrideDatas;
            this.materialKeys = materialKeys;
        }

        public MaterialsOverridePack(UnsafeArray<MaterialOverrideData>.Span defaultData) {
            this.defaultData = defaultData;
            this.overrideDatas = default;
            this.materialKeys = default;
        }

        [UnityEngine.Scripting.Preserve]
        public readonly int FindMaterialIndex(string key) {
            for (var i = 0u; i < materialKeys.Length; i++) {
                if (materialKeys[i].Equals(key)) {
                    return (int)i;
                }
            }
            return -1;
        }
    }
}
