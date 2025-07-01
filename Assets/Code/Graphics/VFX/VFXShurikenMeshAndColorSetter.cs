using UnityEngine;

namespace Awaken.TG.Graphics.VFX
{
    public class VFXShurikenMeshAndColorSetter : MonoBehaviour{

        [SerializeField] bool _configureOnAwake;

        ParticleSystem[] _childrenParticleSytems;

		private void Awake() {
            if (_configureOnAwake)
                SetMeshRenderer();
		}

		public void SetMeshRenderer() {
            _childrenParticleSytems = GetComponentsInChildren<ParticleSystem>();
            var parentMeshRenderer = GetComponentInParent<MeshRenderer>();

            if (parentMeshRenderer != null){
                foreach (var ps in _childrenParticleSytems){
                    var shape = ps.shape;
                    shape.meshRenderer = parentMeshRenderer;
                }
            }
        }

        public void SetParticlesColors(Color c) {
            foreach (var ps in _childrenParticleSytems) {
                var main = ps.main;
                main.startColor = c;
            }
		}
    }
}
