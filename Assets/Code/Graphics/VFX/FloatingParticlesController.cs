using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    public class SphereColliderVFXController : MonoBehaviour {
        public VisualEffect visualEffect;
        public string ashIntensity = "AshIntensity";
        public Volume volume;
        public SphereCollider sphereCollider;

        void Start() {
            sphereCollider = GetComponent<SphereCollider>();

            if (!sphereCollider.isTrigger) {
                Debug.LogWarning("SphereCollider musi być ustawiony jako Trigger.");
                sphereCollider.isTrigger = true;
            }

            if (volume == null) {
                Debug.LogWarning("Brak poprawnego Volume lub VolumeProfile. Sprawdź konfigurację.");
            }
        }

        void OnTriggerStay(Collider other) {
            Vector3 colliderCenter = transform.position + sphereCollider.center;
            float distanceToCenter = Vector3.Distance(colliderCenter, other.transform.position);
            float intensity = Mathf.Clamp01(1f - distanceToCenter / sphereCollider.radius);

            if (visualEffect != null) visualEffect.SetFloat(ashIntensity, intensity);

            if (volume != null) {
                volume.weight = intensity;
            } else {
                Debug.LogWarning("Volume lub VolumeProfile jest niedostępne.");
            }
        }

        void OnTriggerExit(Collider other) {
            if (visualEffect != null) visualEffect.SetFloat(ashIntensity, 0f);

            if (volume != null) {
                volume.weight = 0f;
            } else {
                Debug.LogWarning("Volume lub VolumeProfile jest niedostępne.");
            }
        }
    }
}