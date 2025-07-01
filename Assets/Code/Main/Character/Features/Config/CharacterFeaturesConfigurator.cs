using System;
using System.Linq;
using Awaken.Kandra;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features.Config {
    [RequireComponent(typeof(KandraRenderer)), ExecuteInEditMode]
    public class CharacterFeaturesConfigurator : MonoBehaviour {
        [SerializeField, InlineEditor, PropertyOrder(10)]
        BlendShapeConfigSO blendShapeConfigs;
        /// <summary>
        /// Whether BlendShapeConfig lock should act as select instead of exclude 
        /// </summary>
        [ShowInInspector, OnValueChanged(nameof(LockAsSelectValueChanged)), Tooltip("Whether BlendShapeConfig lock should act as select instead of exclude")] 
        bool _lockAsSelect = false;

        [SerializeField] KandraRenderer kandraRenderer;

        int BlendShapeCount => kandraRenderer.BlendshapesCount;

        public BlendShapeConfigSO BlendShapeConfigs {
            get {
                if (blendShapeConfigs == null) return null;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(blendShapeConfigs);
#endif
                return blendShapeConfigs;
            }
            set => blendShapeConfigs = value;
        }

        // === Buttons
        [Button("Reset BlendShapes", ButtonStyle.FoldoutButton)]
        void SetFlatValue(float valueToResetTo, [MinMaxSlider(0, nameof(BlendShapeCount), true)] Vector2Int rangeToReset, string lastMemberToReset = "") {
            //Ignore 0 range if last member is set
            if (rangeToReset == Vector2Int.zero && !string.IsNullOrWhiteSpace(lastMemberToReset) && BlendShapeConfigs.configs.Any(x => x.targetBlendShape.Contains(lastMemberToReset))) {
                rangeToReset = new Vector2Int(0, BlendShapeCount);
            }
            
            for (var i = (ushort)rangeToReset.x; i < rangeToReset.y; i++) {
                kandraRenderer.SetBlendshapeWeight(i, valueToResetTo);
                if (!string.IsNullOrWhiteSpace(lastMemberToReset) && kandraRenderer.GetBlendshapeName(i).Contains(lastMemberToReset)) {
                    break;
                }
            }
        }
        
        [Button(ButtonSizes.Large), PropertyOrder(-3), Tooltip("Use the selected blend shape config to randomize value on the skinned mesh renderer.")]
        public void Randomize() {
            BlendShapeUtils.ApplyShapes(kandraRenderer, BlendShapeUtils.RandomizeWithParams(BlendShapeConfigs, _lockAsSelect, false));
        }

        [Button, ButtonGroup("Tools")]
        public void LoadShapes() {
            for (ushort i = 0; i < kandraRenderer.BlendshapesCount; i++) {
                string blendShapeName = kandraRenderer.GetBlendshapeName(i);
                if (BlendShapeConfigs.configs.Find(x => x.targetBlendShape == blendShapeName) == null) {
                    BlendShapeConfigs.configs.Insert(i - 1, new BlendShapeConfig { targetBlendShape = blendShapeName });
                }
            }
        }
        
        [Button, GUIColor(1, 0, 0), ButtonGroup("Tools")]
        void ResetBlendShapeList() {
            if (BlendShapeConfigs == null) return;
            BlendShapeConfigs.configs.Clear();
            
            for (ushort i = 0; i < kandraRenderer.BlendshapesCount; i++) {
                string blendShapeName = kandraRenderer.GetBlendshapeName(i);
                BlendShapeConfigs.configs.Add(new BlendShapeConfig { targetBlendShape = blendShapeName });
            }
        }

        [Button]
        void LoadTargetsFromBlendShapes() {
            for (ushort i = 0; i < kandraRenderer.BlendshapesCount; i++) {
                var blendShapeName = kandraRenderer.GetBlendshapeName(i);
                var config = BlendShapeConfigs.configs.Find(x => x.targetBlendShape == blendShapeName);
                if (config != null) {
                    config.targetPoint = kandraRenderer.GetBlendshapeWeight(i);
                }
            }
        }

        // === Unity Event data handling
        void Awake() {
            OnValidate();
        }

        /// <summary>
        /// Reads blend shapes when new SO data container is attached
        /// </summary>
        void OnValidate() {
            kandraRenderer = GetComponent<KandraRenderer>();
            if (Application.isPlaying) return;
            if (BlendShapeConfigs == null) return;
            if (BlendShapeConfigs.configs.Count > 0) {
                for (int i = BlendShapeConfigs.configs.Count - 1; i >= 0; i--) {
                    var config = BlendShapeConfigs.configs[i];
                    if (kandraRenderer.GetBlendshapeIndex(config.targetBlendShape) == -1) {
                        BlendShapeConfigs.configs.RemoveAt(i);
                    }
                }
            } else {
                ResetBlendShapeList();
            }
        }

        // === Odin only
        void LockAsSelectValueChanged() {
            BlendShapeConfig.inverseLock = _lockAsSelect;
        }
    }

    [Serializable]
    public class BlendShapeConfig {
        [DisplayAsString, Indent]
        public string targetBlendShape;
        [SerializeField, TableColumnWidth(25, false), Tooltip("Active")]
        bool a;
        [SerializeField, TableColumnWidth(25, false), Tooltip("Lock"), ShowIf(nameof(a)), GUIColor("$"+nameof(LockColor))]
        bool l;
        [Range(0, 50), ShowIf(nameof(a))]
        public float bias = 16.66666666f;
        [Range(0, 100), ShowIf(nameof(a))]
        public float targetPoint = 50f;
        [SerializeField, TableColumnWidth(25, false), Tooltip("Allow rare extreme values"), ShowIf(nameof(a))]
        bool e;
        // === Name helpers
        public bool Active => a;
        public bool Locked => l;
        public bool Extremes => e;
        
        // === Odin only
        public static bool inverseLock = false;
        static Color LockColor() {
            return inverseLock ? Color.red : Color.white;
        }
    }
}