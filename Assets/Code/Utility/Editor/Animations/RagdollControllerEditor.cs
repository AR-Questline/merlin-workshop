using System.Collections.Generic;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.Utility.Editor.Animations {
    [CustomEditor(typeof(RagdollController))]
    public class RagdollControllerEditor : UnityEditor.Editor {
        static HashSet<RagdollController> s_ragdollApplied = new HashSet<RagdollController>();

        SerializedProperty _ragdollSetupProp;
        SerializedProperty _rootBoneProp;

        RagdollController Controller => (RagdollController)target;

        float _linearDragSetup = 0.5f;
        float _angularDrag = 1.25f;

        void OnEnable() {
            _ragdollSetupProp = serializedObject.FindProperty("ragdollSetup");
            _rootBoneProp = serializedObject.FindProperty("rootBone");
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        void OnDisable() {
            PrefabStage.prefabSaving -= OnPrefabSaving;
        }

        public override void OnInspectorGUI() {
            var isCurrentPrefabStage = true;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || !prefabStage.IsPartOfPrefabContents(Controller.gameObject)) {
                isCurrentPrefabStage = false;
            }

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(!isCurrentPrefabStage);

            EditorGUI.BeginDisabledGroup(s_ragdollApplied.Contains(Controller));
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_ragdollSetupProp);
            if (EditorGUI.EndChangeCheck()) {
                if (_ragdollSetupProp.objectReferenceValue != null) {
                    var so = (RagdollSetupSO)_ragdollSetupProp.objectReferenceValue;
#if !ADDRESSABLES_BUILD
                    _rootBoneProp.objectReferenceValue = Controller.gameObject.FindChildRecursively(so.boneNames[0]);
#endif
                    serializedObject.ApplyModifiedProperties();

                    Controller.RemoveRagdoll();
                }
            }
            if (_ragdollSetupProp.objectReferenceValue == null) {
                if (GUILayout.Button("New")) {
                    CreateNewRagdollSetupSO();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_rootBoneProp);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

            if (Controller.ragdollSetup != null) {
                EditorGUILayout.Space();
                if (s_ragdollApplied.Contains(Controller)) {
                    EditorGUILayout.BeginVertical("Box");
                    if (GUILayout.Button("Drag Setup")) {
                        SetupDrag();
                    }
                    ++EditorGUI.indentLevel;

                    _linearDragSetup = EditorGUILayout.FloatField("Linear Drag", _linearDragSetup);
                    _angularDrag = EditorGUILayout.FloatField("Angular Drag", _angularDrag);

                    --EditorGUI.indentLevel;
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Readback Data To SO")) {
                        ReadbackToSO();
                    }
                    if (GUILayout.Button("Remove Ragdoll")) {
                        Controller.RemoveRagdoll();
                        s_ragdollApplied.Remove(Controller);
                    }
                } else {
                    if (GUILayout.Button("DebugApply Ragdoll")) {
                        Controller.ApplyRagdoll();
                        EditorSceneManager.MarkSceneDirty(Controller.gameObject.scene);
                        EditorUtility.SetDirty(Controller.gameObject);
                        s_ragdollApplied.Add(Controller);
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        void OnPrefabSaving(GameObject prefab) {
            var controller = prefab.GetComponent<RagdollController>();
            if (!controller) {
                return;
            }

            if (s_ragdollApplied.Contains(controller)) {
                bool apply = EditorUtility.DisplayDialog(
                    "Ragdoll Applied",
                    "Ragdoll is applied. What do you want to do before exiting prefab?",
                    "Apply changes to config and remove ragdoll",
                    "Remove ragdoll"
                    );
                if (apply) {
                    ReadbackToSO();

                }
                controller.RemoveRagdoll();
                EditorSceneManager.MarkSceneDirty(prefab.scene);
                EditorUtility.SetDirty(prefab);
                s_ragdollApplied.Remove(controller);
            }
        }

        void ReadbackToSO() {
            CacheRagdollData(Controller.ragdollSetup);
#if !ADDRESSABLES_BUILD
            Controller.rootBone = Controller.gameObject.FindChildRecursively(Controller.ragdollSetup.boneNames[0]);
#endif
            EditorUtility.SetDirty(Controller.ragdollSetup);
            AssetDatabase.SaveAssets();
        }

        void SetupDrag() {
            var root = Controller.transform;
            var bones = new List<Transform>();
            var boneNames = new List<string>();
            var configs = new List<RagdollBoneConfig>();
            float totalMass = 0f;
            uint rigidbodyCount = 0;
            TraverseBones(root, bones, boneNames, configs, ref totalMass, ref rigidbodyCount);

            foreach (var bone in bones) {
                var rb = bone.GetComponent<Rigidbody>();
                if (rb == null) {
                    continue;
                }

                rb.linearDamping = _linearDragSetup;
                rb.angularDamping = _angularDrag;
            }
        }

        void CacheRagdollData(RagdollSetupSO so) {
            var root = Controller.transform;
            var bones = new List<Transform>();
            var boneNames = new List<string>();
            var configs = new List<RagdollBoneConfig>();
            float totalMass = 0f;
            uint rigidbodyCount = 0;
            TraverseBones(root, bones, boneNames, configs, ref totalMass, ref rigidbodyCount);
            so.wholeMass = totalMass;
            so.rigidBodyCount = rigidbodyCount;
            so.boneConfigs = configs.ToArray();
#if !ADDRESSABLES_BUILD
            so.boneNames = boneNames.ToArray();
#endif
        }

        void TraverseBones(Transform bone, List<Transform> bones, List<string> boneNames, List<RagdollBoneConfig> configs, ref float totalMass, ref uint rigidbodyCount) {
            if (bone.gameObject.layer == RenderLayers.Ragdolls) {
                bones.Add(bone);
                boneNames.Add(bone.name);
                var rb = bone.GetComponent<Rigidbody>();
                if (rb != null) {
                    totalMass += rb.mass;
                    ++rigidbodyCount;
                }
                var config = new RagdollBoneConfig();
                RagdollBoneConfig.EditorAccess.Save(ref config, bone);
                configs.Add(config);
            }

            for (int i = 0; i < bone.childCount; i++) {
                TraverseBones(bone.GetChild(i), bones, boneNames, configs, ref totalMass, ref rigidbodyCount);
            }

            if (bone.gameObject.layer == RenderLayers.Ragdolls) {
                RagdollBoneConfig.EditorAccess.Clear(bone);
            }
        }

        void CreateNewRagdollSetupSO() {
            string path = EditorUtility.SaveFilePanelInProject("Create RagdollSetupSO", Controller.name + "_RagdollConfig", "asset", "Choose location", "Assets/Data/Ragdolls");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            var so = ScriptableObject.CreateInstance<RagdollSetupSO>();
            CacheRagdollData(so);
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();

            Controller.ragdollSetup = so;
#if !ADDRESSABLES_BUILD
            Controller.rootBone = Controller.gameObject.FindChildRecursively(so.boneNames[0]);
#endif
        }
    }
}
