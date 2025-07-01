using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Graphics {
    public class BoneWindow : OdinEditorWindow {
        [InfoBox("Has null bones", InfoMessageType.Error, nameof(HasNullBones))]
        [InfoBox("Has no bones", InfoMessageType.Error, nameof(HasNoBones))]
        [SerializeField, OnValueChanged(nameof(OnSkinnedMeshRendererChanged))] SkinnedMeshRenderer skinnedRenderer;
        [SerializeField] Transform[] bones;
        
        [MenuItem("TG/Assets/Mesh/Bone Window")]
        static void ShowWindow() {
            GetWindow<BoneWindow>().Show();
        }
        
        void OnSkinnedMeshRendererChanged() {
            bones = skinnedRenderer?.bones ?? Array.Empty<Transform>();
        }

        bool HasNullBones() {
            if (skinnedRenderer == null) {
                return false;
            }
            foreach (var bone in bones) {
                if (bone == null) {
                    return true;
                }
            }
            return false;
        }

        bool HasNoBones() {
            if (skinnedRenderer == null) {
                return false;
            }
            return bones.Length == 0;
        }
    }
}