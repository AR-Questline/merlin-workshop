using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX{
    public class BakeSkinnedMeshVertexPositions : MonoBehaviour{
        [UnityEngine.Scripting.Preserve] public SkinnedMeshRenderer skinnedMesh;
        [UnityEngine.Scripting.Preserve] public VisualEffect vfxGraph;
        [UnityEngine.Scripting.Preserve] public float refreshRate = 0.02f;
        [UnityEngine.Scripting.Preserve] public int frames = 120;
        [UnityEngine.Scripting.Preserve] public bool loop = true;

        void Awake() {
            Log.Important?.Error($"{nameof(BakeSkinnedMeshVertexPositions)} is obsolete. If you need to use it, please contact the developers.");
        }

        // void OnEnable() {
        //     StopAllCoroutines();
        //     StartCoroutine(UpdateVFXGraph());
        // }
        //
        // IEnumerator UpdateVFXGraph() {
        //     var wait = new WaitForSeconds(refreshRate);
        //     int i = 0;
        //     Mesh bakingMesh = new Mesh();
        //     Mesh effectMesh = new Mesh();
        //     List<Vector3> vertices = new List<Vector3>(skinnedMesh.sharedMesh.vertexCount+1);
        //     while (loop || i < frames) {
        //         if (vfxGraph) {
        //             i++;
        //             bakingMesh.Clear();
        //             skinnedMesh.BakeMesh(bakingMesh);
        //             bakingMesh.GetVertices(vertices);
        //
        //             effectMesh.Clear();
        //             effectMesh.SetVertices(vertices);
        //             vfxGraph.SetMesh("Mesh", effectMesh);
        //         }
        //         yield return wait;
        //     }
        // }
    }
}