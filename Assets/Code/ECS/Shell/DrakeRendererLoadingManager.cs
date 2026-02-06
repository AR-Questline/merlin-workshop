using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    [Il2CppEagerStaticClassConstruction]
    public class DrakeRendererLoadingManager : IAddressableMaterialsLoadingEventsProvider {
        public List<AddressableLoadingData<Mesh>> MeshLoadingData;
        public List<AddressableLoadingData<Material>> MaterialLoadingData;
        public event Action<string> OnStartedLoadingMaterial;
        public event Action<string> OnUnloadingMaterial;
        public event Action<string, Material> OnLoadedMaterial;

        public bool TryGetLoadedMaterial(ushort index, out string runtimeKey, out Material material) {
            throw new NotImplementedException();
        }

        public struct AddressableLoadingData<T> where T : Object {
            public readonly string key;
            public AsyncOperationHandle<T> loadingHandle;
            public ushort counter;

            public AddressableLoadingData(string key) {
                throw new NotImplementedException();
            }
        }

        public class RuntimeMaterialOperation : AsyncOperationBase<Material> {
            protected override float Progress => throw new NotImplementedException();

            public RuntimeMaterialOperation(Material material) {
                throw new NotImplementedException();
            }

            protected override void Execute() {
                throw new NotImplementedException();
            }
        }
    }
}