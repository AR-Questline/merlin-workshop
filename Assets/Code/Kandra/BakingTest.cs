using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Kandra {
    public class BakingTest : MonoBehaviour {
        public KandraRenderer kandraRenderer;

        [Button]
        void Bake() {
            var bakedGo = new GameObject(kandraRenderer.name + " Baked");
            var bakedMesh = kandraRenderer.BakePoseMesh();
            var bakedRenderer = bakedGo.AddComponent<MeshRenderer>();
            bakedRenderer.sharedMaterials = kandraRenderer.rendererData.materials;
            var bakedFilter = bakedGo.AddComponent<MeshFilter>();
            bakedFilter.sharedMesh = bakedMesh;
        }
    }
}