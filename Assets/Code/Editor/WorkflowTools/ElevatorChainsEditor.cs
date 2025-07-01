using Awaken.TG.Main.Locations.Elevator;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    [CustomEditor(typeof(ElevatorChains))]
    public class ElevatorChainsEditor : OdinEditor {
        ElevatorChains Target => (ElevatorChains) target;
        GameObject Chain => Target.chain;

        GameObject _loadedChainAsset;
        GameObject _chainInstance;

        protected override void OnEnable() {
            base.OnEnable();
            GenerateChainInstance();
        }

        void GenerateChainInstance() {
            if (Application.isPlaying) return;
            _loadedChainAsset = Chain;
            if (Chain == null) {
                return;
            }

            _chainInstance = Instantiate(Chain, Target.transform);
            _chainInstance.hideFlags = HideFlags.HideAndDontSave;
            _chainInstance.transform.position = Vector3.zero;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (Chain != _loadedChainAsset) {
                if (_chainInstance != null) {
                    CleanupInstance();
                }

                GenerateChainInstance();
            }
            
            // No chain return
            if (_chainInstance == null) return;

            var chainCollider = _chainInstance.GetComponentInChildren<Collider>();
            if (!chainCollider) {
                return;
            }

            Bounds bounds = chainCollider.bounds;
            float newChainHeight = bounds.max.y - bounds.min.y;
            if (!Mathf.Approximately(newChainHeight, Target.singleChainHeight)) {
                Target.singleChainHeight = newChainHeight;
                EditorUtility.SetDirty(Target);
            }
            
            if (Target.singleChainHeight <= 0) {
                return;
            }

            if (Target.EDITOR_InitializedSlotsCount != Target.chainPositions.Length) {
                Target.EDITOR_Clear();
                Target.InitChainContainers();
            }

            Target.platformHeight = Target.transform.position.y;
            Target.EDITOR_HandleChainGeneration();
        }

        protected override void OnDisable() {
            base.OnDisable();
            CleanupInstance();
        }

        void CleanupInstance() {
            DestroyImmediate(_chainInstance);
            if (Target != null) {
                Target.EDITOR_Clear();
            }
        }
    }
}