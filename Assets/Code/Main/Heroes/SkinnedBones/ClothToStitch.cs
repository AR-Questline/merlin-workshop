using Awaken.Kandra;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.SkinnedBones {
    [ExecuteInEditMode]
    public class ClothToStitch : MonoBehaviour {
        [SerializeField, OnValueChanged(nameof(Refresh))] GameObject cloth;

        [ShowInInspector, ReadOnly] GameObject _instance;
        
        void OnEnable() {
            if (!cloth) {
                return;
            }
            var rig = GetComponentInParent<KandraRig>();
            if (!rig) {
                Log.Minor?.Error($"There is no KandraRig for {gameObject}", gameObject);
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                _instance = Instantiate(cloth, transform);
                _instance.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
            } else
#endif
            {
                _instance = ClothStitcher.Stitch(cloth, rig);
            }
        }

        void OnDisable() {
            if (_instance) {
                GameObjects.DestroySafely(_instance, true);
                _instance = null;
            }
        }
        
        void Refresh() {
            OnDisable();
            OnEnable();
        }
    }
}