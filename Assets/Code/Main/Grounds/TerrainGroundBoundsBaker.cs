using System.Collections.Generic;
using Awaken.CommonInterfaces.Assets;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.Grounds {
    public class TerrainGroundBoundsBaker : MonoBehaviour, IEditorOnlyMonoBehaviour {
        [SerializeField] Impl impl;

        [SerializeField] [ShowIf(nameof(_showBoth))]
        float meshHeightOffset = 0.01f;

        readonly Dictionary<Transform, Vector3> _originalPositions = new();
        
        bool _hasBaked;
        bool _showBoth;

        void OnSceneSaving(Scene scene, string path) {
#if UNITY_EDITOR
            if (this == null) {
                EditorSceneManager.sceneSaving -= OnSceneSaving;
                return;
            }

            if (gameObject.scene == scene)
                foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
                    if (renderer.enabled) {
                        EditorUtility.DisplayDialog(
                            "Save Blocked",
                            "Scene save was prevented because terrain preview is active.",
                            "Switch to Unity Terrains"
                        );
                        SwitchToTerrains();
                    }
#endif
        }

        public void Bake(GroundBounds groundBounds) {
            Bake(groundBounds: groundBounds, true);
        }

        [HorizontalGroup("BakeRow", 0.5f)]
        [GUIColor(0.45f, 0.85f, 0.45f)]
        [Button(size: ButtonSizes.Large, Name = "Bake For Editor")]
#if UNITY_EDITOR
        [MenuItem("TG/Grounds/Bake Terrain For Editor _b")]
#endif
        static void BakeForEditor() {
            TerrainGroundBoundsBaker baker = FindAnyObjectByType<TerrainGroundBoundsBaker>();
            if (baker == null) return;

            foreach (LODGroup lod in baker.GetComponentsInChildren<LODGroup>()) DestroyImmediate(obj: lod.gameObject);

#if UNITY_EDITOR
            EditorSceneManager.sceneSaving += baker.OnSceneSaving;
#endif
            baker.Bake(FindAnyObjectByType<GroundBounds>(), false);
            baker.Switch(true);
        }

        [HorizontalGroup("BakeRow", 0.5f)]
        [GUIColor(0.9f, 0.4f, 0.4f)]
        [Button(size: ButtonSizes.Large, Name = "Bake For Build")]
        void BakeForBuild() {
            Bake(FindAnyObjectByType<GroundBounds>(), true);
        }

        [HorizontalGroup("SwitchRow", Width = 0.33f)]
        [EnableIf(nameof(_hasBaked))]
        [Button(size: ButtonSizes.Large, Name = "Switch To Terrains")]
        void SwitchToTerrains() {
            _showBoth = false;
            Switch(false);
        }

        [HorizontalGroup("SwitchRow", Width = 0.33f)]
        [EnableIf(nameof(_hasBaked))]
        [Button(size: ButtonSizes.Large, Name = "Switch To Meshes")]
        void SwitchToMeshes() {
            _showBoth = false;
            Switch(true);
        }

        [HorizontalGroup("SwitchRow", Width = 0.33f)]
        [EnableIf(nameof(_hasBaked))]
        [Button(size: ButtonSizes.Large, Name = "Switch To Both")]
        void SwitchToBoth() {
            _showBoth = true;
            Switch(true);
        }

        void Bake(GroundBounds groundBounds, bool forBuild) {
            if (!groundBounds) return;

#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabInstance(componentOrGameObject: gameObject)) {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(componentOrGameObject: gameObject);
                PrefabUtility.UnpackPrefabInstance(instanceRoot: root, unpackMode: PrefabUnpackMode.Completely,
                    action: InteractionMode.AutomatedAction);
            }
#endif
            if (impl) {
                impl.Bake(groundBounds: groundBounds, this, forBuild: forBuild);
                _hasBaked = true;
            }
        }

        void Switch(bool meshes) {
            if (_showBoth) {
                foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>()) {
                    renderer.enabled = true;
                    Transform t = renderer.transform;
                    if (!_originalPositions.ContainsKey(key: t)) _originalPositions[key: t] = t.localPosition;
                    t.localPosition = _originalPositions[key: t] + Vector3.up * meshHeightOffset;
                }

                foreach (Terrain terrain in GetComponentsInChildren<Terrain>()) terrain.enabled = true;
            } else {
                foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>()) {
                    renderer.enabled = meshes;
                    Transform t = renderer.transform;
                    if (_originalPositions.ContainsKey(key: t)) t.localPosition = _originalPositions[key: t];
                }

                foreach (Terrain terrain in GetComponentsInChildren<Terrain>()) terrain.enabled = !meshes;
            }
        }

        public abstract class Impl : ScriptableObject {
            public abstract void Bake(GroundBounds groundBounds, TerrainGroundBoundsBaker baker, bool forBuild);
        }
    }
}