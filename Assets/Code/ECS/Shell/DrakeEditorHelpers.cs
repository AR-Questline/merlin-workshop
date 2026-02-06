using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeEditorHelpers {
        public static bool Bake(DrakeToBake drakeToBake) {
            throw new NotImplementedException();
        }

        public static bool Bake(DrakeLodGroup drakeLodGroup, MeshRenderer meshRenderer) {
            throw new NotImplementedException();
        }

        public static bool Bake(DrakeLodGroup drakeLodGroup, LODGroup lodGroup) {
            throw new NotImplementedException();
        }

        public static void Unbake(DrakeLodGroup drakeLodGroup) {
            throw new NotImplementedException();
        }

        public static void SpawnAuthoring(DrakeLodGroup drakeLodGroup, GameObject unbakeTarget = null) {
            throw new NotImplementedException();
        }

        public static T LoadAsset<T>(AssetReference assetReference) where T : Object {
            throw new NotImplementedException();
        }

        public static void MeshStats(Mesh mesh, out int vertexCount, out uint triangleCount, out int subMeshCount) {
            throw new NotImplementedException();
        }
    }
}