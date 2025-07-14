using System;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Helpers;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    public class KandraMeshReplacer : AREditorWindow {
        [SerializeField] ReplaceData[] replaces = Array.Empty<ReplaceData>();

        [SerializeField] bool closeOnReplace;

        string _error;

        Vector2 _replacesScrollPosition;

        protected override void OnEnable() {
            base.OnEnable();

            AddButton("Replace", Replace, () => replaces.Length > 0);
            AddCustomDrawer(nameof(replaces), DrawReplaces);
        }

        protected override void OnGUI() {
            EditorGUI.BeginChangeCheck();

            base.OnGUI();

            if (EditorGUI.EndChangeCheck()) {
                OnInputChanged();
            }

            if (!_error.IsNullOrWhitespace()) {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }
        }

        void DrawReplaces(SerializedProperty replacesProp) {
            if (replacesProp.arraySize == 0) {
                return;
            }

            for (int i = 0; i < replacesProp.arraySize; i++) {
                var replace = replacesProp.GetArrayElementAtIndex(i);
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");

                var rendererProp = replace.FindPropertyRelative(nameof(ReplaceData.renderer));
                EditorGUILayout.PropertyField(rendererProp);

                var meshProp = replace.FindPropertyRelative(nameof(ReplaceData.mesh));
                EditorGUILayout.PropertyField(meshProp);

                var rootBoneNameProp = replace.FindPropertyRelative(nameof(ReplaceData.rootBoneName));
                var rootBoneNameValue = rootBoneNameProp.stringValue;
                var possibleRootBones = KandraMeshBaker.GetPossibleRootBones(meshProp.objectReferenceValue as Mesh);
                var index = Array.IndexOf(possibleRootBones, rootBoneNameValue);
                var value = EditorGUILayout.Popup(rootBoneNameProp.displayName, index, possibleRootBones);
                var newRootBoneName = value >= 0 ? possibleRootBones[value] : string.Empty;
                if (newRootBoneName != rootBoneNameValue) {
                    rootBoneNameProp.stringValue = newRootBoneName;
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        void Replace() {
            if (Replace(replaces, out _error)) {
                if (closeOnReplace) {
                    Close();
                }
            }
        }

        public static bool Replace(ReplaceData[] replaces, out string error) {
            if (replaces.Length == 0) {
                error = null;
                return true;
            }

            var rig = replaces[0].renderer?.rendererData.rig;
            for (int i = 0; i < replaces.Length; i++) {
                ref readonly var replace = ref replaces[i];
                if (replace.renderer == null) {
                    error = $"Renderer is null for replace {i}";
                    Log.Important?.Error(error, logOption: LogOption.NoStacktrace);
                    return false;
                }
                if (replace.renderer.rendererData.rig == null) {
                    error = $"KandraRig is null for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, replace.renderer, LogOption.NoStacktrace);
                    return false;
                }
                if (replace.renderer.rendererData.rig != rig) {
                    error = $"KandraRig mismatch for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, replace.renderer, LogOption.NoStacktrace);
                    return false;
                }
                if (replace.mesh == null) {
                    error = $"Mesh is null for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, logOption: LogOption.NoStacktrace);
                    return false;
                }
                if (replace.rootBoneName.IsNullOrWhitespace()) {
                    error = $"Root bone name is null or empty for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, logOption: LogOption.NoStacktrace);
                    return false;
                }
            }

            var replacedIntermediateData = new ReplaceIntermediateData[replaces.Length];
            for (int i = 0; i < replaces.Length; i++) {
                ref readonly var replace = ref replaces[i];
                ref var intermediate = ref replacedIntermediateData[i];
                string nestedError;
                if (!KandraMeshBaker.TryGetFbxRenderer(replace.mesh, out intermediate.fbx, out intermediate.skinnedRenderer, out nestedError)) {
                    error = $"{nestedError} for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, logOption: LogOption.NoStacktrace);
                    return false;
                }
                if (!KandraMeshBaker.TryGetRootBone(intermediate.fbx, intermediate.skinnedRenderer, KandraMeshBaker.ValidateRootBoneName(replace.rootBoneName), out intermediate.replacingRootBone, out intermediate.replacingRootBoneIndex, out nestedError)) {
                    error = $"{nestedError} for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, logOption: LogOption.NoStacktrace);
                    return false;
                }
            }


            var possibleBones = GameObjects.BreadthFirst(rig.transform).ToArray();
            var possibleBoneNames = ArrayUtils.Select(possibleBones, bone => bone.name);
            
            var usedBonesUnion = new UnsafeBitmask((uint)possibleBones.Length, ARAlloc.Temp);
            var allRenderers = rig.GetComponentsInChildren<KandraRenderer>().Where(r => r.rendererData.rig == rig).ToArray();
            var usedBones = new Transform[allRenderers.Length][];
            var rootBones = new Transform[allRenderers.Length];

            for (int i = 0; i < allRenderers.Length; i++) {
                var childRenderer = allRenderers[i];

                bool isBeingReplaced = false;
                for (int j = 0; j < replaces.Length; j++) {
                    if (replaces[j].renderer == childRenderer) {
                        isBeingReplaced = true;
                        replacedIntermediateData[j].rendererIndex = i;
                        break;
                    }
                }
                if (isBeingReplaced) {
                    continue;
                }

                usedBones[i] = new Transform[childRenderer.rendererData.bones.Length];
                rootBones[i] = rig.bones[childRenderer.rendererData.rootBone];
                for (int j = 0; j < childRenderer.rendererData.bones.Length; j++) {
                    ushort boneIndex = childRenderer.rendererData.bones[j];
                    var bone = rig.bones[boneIndex];
                    int index = possibleBones.IndexOf(bone);
                    if (index == -1) {
                        error = $"Bone {bone} from renderer {childRenderer} not found in rig";
                        Log.Important?.Error(error, bone, LogOption.NoStacktrace);
                        usedBonesUnion.Dispose();
                        return false;
                    }

                    usedBonesUnion.Up((uint)index);
                    usedBones[i][j] = possibleBones[index];
                }
            }

            for (int i = 0; i < replaces.Length; i++) {
                ref readonly var replace = ref replaces[i];
                ref var intermediate = ref replacedIntermediateData[i];
                if (intermediate.rendererIndex == -1) {
                    error = $"Renderer not found in rig for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, replace.renderer, LogOption.NoStacktrace);
                    usedBonesUnion.Dispose();
                    return false;
                }

                int sameMeshIndex = -1;
                for (int j = 0; j < i; j++) {
                    if (replaces[j].mesh == replace.mesh) {
                        sameMeshIndex = j;
                        break;
                    }
                }
                if (sameMeshIndex != -1) {
                    ref readonly var sameMeshIntermediate = ref replacedIntermediateData[sameMeshIndex];
                    intermediate.kandraMesh = sameMeshIntermediate.kandraMesh;
                    intermediate.replacedRootBoneIndex = sameMeshIntermediate.replacedRootBoneIndex;
                    intermediate.replacedBoneMask = sameMeshIntermediate.replacedBoneMask;
                } else {
                    intermediate.kandraMesh = KandraMeshBaker.Create(replace.mesh, intermediate.replacingRootBoneIndex, out intermediate.replacedRootBindpose, out intermediate.replacedBoneMask);
                }
                
                usedBones[intermediate.rendererIndex] = new Transform[intermediate.replacedBoneMask.CountOnes()];
                
                intermediate.replacedRootBoneIndex = Array.IndexOf(possibleBoneNames, intermediate.replacingRootBone.name);
                if (intermediate.replacedRootBoneIndex == -1) {
                    error = $"Root bone {intermediate.replacingRootBone.name} from fbx not found in rig for replace {i} {replace.renderer.name}";
                    Log.Important?.Error(error, intermediate.replacingRootBone, LogOption.NoStacktrace);
                    usedBonesUnion.Dispose();
                    return false;
                }
                rootBones[intermediate.rendererIndex] = possibleBones[intermediate.replacedRootBoneIndex];
                
                var replacingBones = intermediate.skinnedRenderer.bones;
                int usedBoneIndex = 0;
                for (uint j = 0; j < replacingBones.Length; j++) {
                    if (intermediate.replacedBoneMask[j]) {
                        var boneName = replacingBones[j].name;
                        var boneIndex = Array.IndexOf(possibleBoneNames, boneName);
                        if (boneIndex == -1) {
                            error = $"Bone {boneName} from fbx not found in rig for replace {i} {replace.renderer.name}";
                            Log.Important?.Error(error, intermediate.fbx, LogOption.NoStacktrace);
                            usedBonesUnion.Dispose();
                            intermediate.replacedBoneMask.Dispose();
                            return false;
                        }
                        usedBonesUnion.Up((uint)boneIndex);

                        var boneToAdd = possibleBones[boneIndex];                    
                        usedBones[intermediate.rendererIndex][usedBoneIndex++] = boneToAdd;

                        var parentBone = boneToAdd;
                        while (true) {
                            if (parentBone == possibleBones[intermediate.replacedRootBoneIndex]) {
                                break;
                            }
                            parentBone = parentBone.parent;
                            var parentBoneIndex = Array.IndexOf(possibleBoneNames, parentBone.name);
                            if (parentBoneIndex == -1) {
                                break;
                            }
                            if (usedBonesUnion[(uint)parentBoneIndex]) {
                                break;
                            }
                            usedBonesUnion.Up((uint)parentBoneIndex);
                        }
                    }
                }
            }

            for (int i = 0; i < replaces.Length; i++) {
                replacedIntermediateData[i].replacedBoneMask.Dispose();
            }

            var rigBones = new Transform[usedBonesUnion.CountOnes()];
            var rigBoneIndex = 0;
            foreach (var i in usedBonesUnion.EnumerateOnes()) {
                rigBones[rigBoneIndex++] = possibleBones[i];
            }
            usedBonesUnion.Dispose();
            
            rig.bones = rigBones;
            rig.boneNames = ArrayUtils.Select(rigBones, b => new FixedString64Bytes(b.name));
            rig.boneParents = ArrayUtils.Select(rigBones, b => {
                var parent = b.parent;
                if (parent) {
                    return (ushort)Array.IndexOf(rigBones, parent);
                } else {
                    return (ushort)0xFFFF;
                }
            });
            rig.baseBoneCount = (ushort)rigBones.Length;
            EditorUtility.SetDirty(rig);
            
            for (int i = 0; i < allRenderers.Length; i++) {
                ref var data = ref allRenderers[i].rendererData;
                data.bones = ArrayUtils.Select(usedBones[i], b => (ushort)Array.IndexOf(rigBones, b));
                data.rootBone = (ushort)Array.IndexOf(rigBones, rootBones[i]);
                EditorUtility.SetDirty(allRenderers[i]);
            }

            for (int i = 0; i < replaces.Length; i++) {
                ref readonly var replace = ref replaces[i];
                ref var intermediate = ref replacedIntermediateData[i];
                ref var replacedData = ref replace.renderer.rendererData;
                replacedData.mesh = intermediate.kandraMesh;
                replacedData.EDITOR_sourceMesh = replace.mesh;
                replacedData.rootBoneMatrix = intermediate.replacedRootBindpose;
                replace.renderer.rendererData.constantBlendshapes?.Validate(replace.renderer);
            }

            error = null;
            return true;
        }

        [MenuItem("TG/Assets/Kandra/Replace Mesh")]
        static void Open() {
            GetWindow<KandraMeshReplacer>().Show();
        }
        
        [MenuItem("CONTEXT/KandraRenderer/Replace Mesh")]
        static void Replace(MenuCommand command) {
            Replace(command.context as KandraRenderer);
        }

        public static void Replace(KandraRenderer renderer, Mesh mesh = null, string rootBoneName = null) {
            var window = GetWindow<KandraMeshReplacer>();
            window.replaces = new[] { new ReplaceData {
                renderer = renderer,
                mesh = mesh ?? renderer.rendererData.EDITOR_sourceMesh,
                rootBoneName = rootBoneName ?? renderer.rendererData.rig.boneNames[renderer.rendererData.rootBone].ToString()
            } };
            window.closeOnReplace = true;
            window.Show();
        }

        void OnInputChanged() {
            _error = null;
        }
        
        [Serializable]
        public struct ReplaceData {
            public KandraRenderer renderer;
            public Mesh mesh;
            public string rootBoneName;
            
            string[] PossibleRootBones() {
                return KandraMeshBaker.GetPossibleRootBones(mesh);
            }
        }

        struct ReplaceIntermediateData {
            public GameObject fbx;
            public SkinnedMeshRenderer skinnedRenderer;
            public Transform replacingRootBone;
            public int replacingRootBoneIndex;
            public int rendererIndex;
            public KandraMesh kandraMesh;
            public float3x4 replacedRootBindpose;
            public UnsafeBitmask replacedBoneMask;
            public int replacedRootBoneIndex;
        }
    }
}