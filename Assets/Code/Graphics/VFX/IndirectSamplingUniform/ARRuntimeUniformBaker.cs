using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX.IndirectSamplingUniform {
    public class ARRuntimeUniformBaker : MonoBehaviour {
        [Min(1)] public int sampleCount = 16;

        int _bakedMesh;
        GraphicsBuffer _samplesBuffer;
        Bounds _meshBounds;

        ARUniformMeshSampling _sampling;

        public bool IsBaked => _samplesBuffer?.IsValid() == true;
        public GraphicsBuffer SamplesBuffer => _samplesBuffer;
        public Bounds MeshBounds => _meshBounds;

        public void OnDestroy() {
            if (_samplesBuffer != null) {
                _samplesBuffer.Release();
                _samplesBuffer = null;
            }

            _sampling?.Dispose();
        }
        public void Bake(Mesh mesh) {
            if (mesh == null) {
                Log.Important?.Error($"Cannot bake {nameof(ARRuntimeUniformBaker)} with null mesh", this);
                return;
            }
            if (IsBaked) {
                if (_bakedMesh != mesh.GetHashCode()) {
                    Log.Important?.Error($"Cannot bake again {nameof(ARRuntimeUniformBaker)}. It was baked with mesh {_bakedMesh} but we want to bake it for {mesh}", this);
                }
                return;
            }
            _meshBounds = mesh.bounds;

            _sampling = new ARUniformMeshSampling();
            _samplesBuffer = _sampling.StartSampling(mesh, sampleCount);
            _bakedMesh = mesh.GetHashCode();
        }

        public void Progress() {
            _sampling?.UpdateLoading(_samplesBuffer, ref _sampling);
        }
    }
}
