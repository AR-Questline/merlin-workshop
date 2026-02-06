using System;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;

namespace Awaken.ECS.DrakeRenderer.Utilities {
    public struct MaterialsOverridePack {
        public UnsafeArray<MaterialOverrideData>.Span overrideDatas;
        public UnsafeArray<FixedString128Bytes>.Span materialKeys;
        public UnsafeArray<MaterialOverrideData>.Span defaultData;

        public MaterialsOverridePack(UnsafeArray<MaterialOverrideData>.Span overrideDatas,
            UnsafeArray<FixedString128Bytes>.Span materialKeys,
            UnsafeArray<MaterialOverrideData>.Span defaultData = default) {
            throw new NotImplementedException();
        }

        public MaterialsOverridePack(UnsafeArray<MaterialOverrideData>.Span defaultData) {
            throw new NotImplementedException();
        }

        public readonly int FindMaterialIndex(string key) {
            throw new NotImplementedException();
        }
    }
}