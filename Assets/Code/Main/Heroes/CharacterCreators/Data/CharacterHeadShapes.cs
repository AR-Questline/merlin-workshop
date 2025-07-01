using System;
using System.Linq;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character.Features;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    [Serializable]
    public struct CharacterHeadShapes {
        const string ShapeGroup = "Shapes";
        public const int Count = 20;

        [SerializeField, UIAssetReference(AddressableLabels.UI.CharacterCreator)]
        ShareableSpriteReference icon;

        [Space(5)]
        // ReSharper disable InconsistentNaming
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_01;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_02;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_03;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_04;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_05;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_06;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_07;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Base_Face_08;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Nose_01;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Nose_02;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Nose_03;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Nose_04;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Mouth_01;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Mouth_02;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Jaw_01;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Jaw_02;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Eyes_01;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Eyes_02;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Eyes_03;
        [SerializeField, FoldoutGroup(ShapeGroup), Range(-1, 1)] float CC_Ears_01;

        // ReSharper restore InconsistentNaming
        public ShareableSpriteReference Icon => icon;

        public void FillShapesContinuously(BlendShape[] shapes, int startIndex) {
            int index = startIndex;
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_01), CC_Base_Face_01);
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_02), CC_Base_Face_02);
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_03), CC_Base_Face_03);
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_04), CC_Base_Face_04);
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_05), CC_Base_Face_05);
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_06), CC_Base_Face_06);
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_07), CC_Base_Face_07);
            shapes[index++] = new BlendShape(nameof(CC_Base_Face_08), CC_Base_Face_08);
            shapes[index++] = new BlendShape(nameof(CC_Nose_01), CC_Nose_01);
            shapes[index++] = new BlendShape(nameof(CC_Nose_02), CC_Nose_02);
            shapes[index++] = new BlendShape(nameof(CC_Nose_03), CC_Nose_03);
            shapes[index++] = new BlendShape(nameof(CC_Nose_04), CC_Nose_04);
            shapes[index++] = new BlendShape(nameof(CC_Mouth_01), CC_Mouth_01);
            shapes[index++] = new BlendShape(nameof(CC_Mouth_02), CC_Mouth_02);
            shapes[index++] = new BlendShape(nameof(CC_Jaw_01), CC_Jaw_01);
            shapes[index++] = new BlendShape(nameof(CC_Jaw_02), CC_Jaw_02);
            shapes[index++] = new BlendShape(nameof(CC_Eyes_01), CC_Eyes_01);
            shapes[index++] = new BlendShape(nameof(CC_Eyes_02), CC_Eyes_02);
            shapes[index++] = new BlendShape(nameof(CC_Eyes_03), CC_Eyes_03);
            shapes[index++] = new BlendShape(nameof(CC_Ears_01), CC_Ears_01);
        }

#if UNITY_EDITOR
        [ShowInInspector, HideInPlayMode, OnValueChanged(nameof(OnGameObjectChanged)), PropertyOrder(0), HideReferenceObjectPicker] 
        GameObject _copyFromPrefab;

        void OnGameObjectChanged() {
            if (_copyFromPrefab == null) {
                return;
            }
            
            var skinnedMeshRenderer = _copyFromPrefab.GetComponentsInChildren<SkinnedMeshRenderer>()
                .FirstOrDefault(s => s.sharedMesh.blendShapeCount >= Count);
            var kandraRenderer = _copyFromPrefab.GetComponentsInChildren<KandraRenderer>()
                .FirstOrDefault(s => s.BlendshapesCount >= Count);
            if (skinnedMeshRenderer == null && kandraRenderer == null) {
                Log.Important?.Error("Failed to find SkinnedMeshRenderer or KandraRenderer to copy blendshapes from!");
                _copyFromPrefab = null;
                return;
            }
            
            if (skinnedMeshRenderer != null) {
                CopyFrom(skinnedMeshRenderer);
            } else {
                CopyFrom(kandraRenderer);
            }
        }

        void CopyFrom(SkinnedMeshRenderer skinnedMeshRenderer) {
            Mesh mesh = skinnedMeshRenderer.sharedMesh;

            for (int i = 0; i < mesh.blendShapeCount; i++) {
                var blendShapeName = mesh.GetBlendShapeName(i);
                if (!blendShapeName.StartsWith("CC_")) {
                    continue;
                }
                var weight = skinnedMeshRenderer.GetBlendShapeWeight(i);
                OnNewBlendshape(blendShapeName, weight);
            }
        }

        void CopyFrom(KandraRenderer kandraRenderer) {
            for (ushort i = 0; i < kandraRenderer.BlendshapesCount; i++) {
                var blendShapeName = kandraRenderer.GetBlendshapeName(i);
                if (!blendShapeName.StartsWith("CC_")) {
                    continue;
                }
                var weight = kandraRenderer.GetBlendshapeWeight(i);
                OnNewBlendshape(blendShapeName, weight);
            }
        }

        void OnNewBlendshape(string blendShapeName, float weight) {
            switch (blendShapeName) {
                case nameof(CC_Nose_01):
                    CC_Nose_01 = weight;
                    break;
                case nameof(CC_Nose_02):
                    CC_Nose_02 = weight;
                    break;
                case nameof(CC_Nose_03):
                    CC_Nose_03 = weight;
                    break;
                case nameof(CC_Nose_04):
                    CC_Nose_04 = weight;
                    break;
                case nameof(CC_Eyes_01):
                    CC_Eyes_01 = weight;
                    break;
                case nameof(CC_Eyes_02):
                    CC_Eyes_02 = weight;
                    break;
                case nameof(CC_Eyes_03):
                    CC_Eyes_03 = weight;
                    break;
                case nameof(CC_Ears_01):
                    CC_Ears_01 = weight;
                    break;
                case nameof(CC_Mouth_01):
                    CC_Mouth_01 = weight;
                    break;
                case nameof(CC_Mouth_02):
                    CC_Mouth_02 = weight;
                    break;
                case nameof(CC_Jaw_01):
                    CC_Jaw_01 = weight;
                    break;
                case nameof(CC_Jaw_02):
                    CC_Jaw_02 = weight;
                    break;
                case nameof(CC_Base_Face_01):
                    CC_Base_Face_01 = weight;
                    break;
                case nameof(CC_Base_Face_02):
                    CC_Base_Face_02 = weight;
                    break;
                case nameof(CC_Base_Face_03):
                    CC_Base_Face_03 = weight;
                    break;
                case nameof(CC_Base_Face_04):
                    CC_Base_Face_04 = weight;
                    break;
                case nameof(CC_Base_Face_05):
                    CC_Base_Face_05 = weight;
                    break;
                case nameof(CC_Base_Face_06):
                    CC_Base_Face_06 = weight;
                    break;
                case nameof(CC_Base_Face_07):
                    CC_Base_Face_07 = weight;
                    break;
                case nameof(CC_Base_Face_08):
                    CC_Base_Face_08 = weight;
                    break;
            }
        }
#endif
    }
}