using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Awaken.Kandra.Managers {
    public class MaterialBroker {
        public Material GetMaterial(Material material, KandraRenderer debugTarget) {
            throw new NotImplementedException();
        }

        public Material CreateInstanced(Material kandraMaterial, KandraRenderer debugTarget) {
            throw new NotImplementedException();
        }

        public void ReleaseMaterial(Material material, KandraRenderer debugTarget) {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseInstancedMaterial(Material material) {
            throw new NotImplementedException();
        }

        public void Editor_OnMaterialChanged(Material material) {
            throw new NotImplementedException();
        }
    }
}