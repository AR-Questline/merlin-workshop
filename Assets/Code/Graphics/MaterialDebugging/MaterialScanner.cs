using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.MaterialDebugging {
    [RequireComponent(typeof(Camera))]
    public class MaterialScanner : MonoBehaviour {

        static MaterialScanner s_instance;

        [SerializeField] float range = 100;

        IMaterialDebugMode _cachedMode;
        Renderer[] _cachedRenderers;

        [Button]
        void NormalView() {
            Stop();
        }

        [Button]
        void MaterialID() {
            Switch(ModeMaterialID);
        }

        [Button]
        void Coverage() {
            Switch(ModeCoverage);
        }
        
        [Button]
        void Close() {
            Stop();
            s_instance = null;
            Destroy(this);
        }
        
        void Stop() {
            _cachedMode?.Clear(_cachedRenderers);
            _cachedMode = null;
            _cachedRenderers = null;
        }

        void Switch(IMaterialDebugMode mode) {
            if (_cachedMode != null) {
                Stop();
            }
            _cachedRenderers = RenderersToProcess();
            _cachedMode = mode;
            _cachedMode.Init(_cachedRenderers);
        }

        Renderer[] RenderersToProcess() {
            float rangeSq = range * range;
            var center = gameObject.transform.position;
            return FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => (r.gameObject.transform.position - center).sqrMagnitude <= rangeSq)
                .ToArray();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("TG/Debug/Materials")]
        public static void StartDebugMode() {
            if (!Application.isPlaying) return;

            if (s_instance == null) {
                var camera = Camera.main ?? FindAnyObjectByType<Camera>();
                s_instance = camera.gameObject.AddComponent<MaterialScanner>();
                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(s_instance)) { }
            }

            UnityEditor.Selection.SetActiveObjectWithContext(s_instance, s_instance.gameObject);
        }
#endif

        static readonly IMaterialDebugMode
            ModeMaterialID = new MaterialIDMode(),
            ModeCoverage = new Coverage();
    }
}