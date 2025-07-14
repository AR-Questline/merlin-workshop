using System;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(ConstantKandraBlendshapes))]
    public class ConstantKandraBlendshapesEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            var blendshapes = (ConstantKandraBlendshapes)target;
            if (!blendshapes.TryGetComponent<KandraRenderer>(out var renderer)) {
                return;
            }
            var mesh = renderer.rendererData.EDITOR_sourceMesh;
            if (mesh == null) {
                return;
            }

            if (renderer.rendererData.mesh.blendshapesNames.Length == 0) {
                EditorGUILayout.HelpBox("No blendshapes found in the mesh", MessageType.Warning);
                if (GUILayout.Button("Remove empty constant blendshapes")) {
                    DestroyImmediate(target);
                }
                return;
            }

            bool dirty = false;
            ref var savedShapes = ref blendshapes.blendshapes;
            if (NeedSorting(savedShapes)) {
                Array.Sort(savedShapes, (lhs, rhs) => lhs.index.CompareTo(rhs.index));
                dirty = true;
            }
            RemoveDuplicatesAndZeros(ref savedShapes, ref dirty);

            int count = mesh.blendShapeCount;

            RemoveOverRange(ref savedShapes, count, ref dirty);

            int iSavedShape = 0;
            for (ushort meshShape = 0; meshShape < count; meshShape++) {
                var name = mesh.GetBlendShapeName(meshShape);
                if (!name.StartsWith("CC_")) {
                    continue;
                }

                while (iSavedShape < savedShapes.Length && savedShapes[iSavedShape].index < meshShape) {
                    // not CC_ blendshape registered
                    ArrayUtils.RemoveAt(ref savedShapes, iSavedShape);
                    dirty = true;
                }

                if (iSavedShape < savedShapes.Length && savedShapes[iSavedShape].index == meshShape) {
                    // CC_ blendshape registered
                    ref var savedShape = ref savedShapes[iSavedShape];
                    var value = EditorGUILayout.Slider(name, savedShape.value, -1, 1);
                    if (value == 0) {
                        renderer.SetBlendshapeWeight(meshShape, value);
                        ArrayUtils.RemoveAt(ref savedShapes, iSavedShape);
                        iSavedShape--;
                        dirty = true;
                    } else if (savedShape.value != value) {
                        renderer.SetBlendshapeWeight(meshShape, value);
                        savedShape.value = value;
                        dirty = true;
                    }
                    iSavedShape++;
                } else {
                    // CC_ blendshape not registered
                    var value = EditorGUILayout.Slider(name, 0, -1, 1);
                    if (value != 0) {
                        var shape = new ConstantKandraBlendshapes.ConstantBlendshape {
                            index = meshShape,
                            value = value
                        };
                        ArrayUtils.Insert(ref savedShapes, iSavedShape, shape);
                        iSavedShape++;
                        renderer.SetBlendshapeWeight(meshShape, value);
                        dirty = true;
                    }
                }
            }
            
            if (dirty){
                EditorUtility.SetDirty(blendshapes);
                serializedObject.Update();
            }
        }
        
        static bool NeedSorting(ConstantKandraBlendshapes.ConstantBlendshape[] blendshapes) {
            for (int i = 1; i < blendshapes.Length; i++) {
                if (blendshapes[i - 1].index > blendshapes[i].index) {
                    return true;
                }
            }
            return false;
        }

        static void RemoveDuplicatesAndZeros(ref ConstantKandraBlendshapes.ConstantBlendshape[] blendshapes, ref bool dirty) {
            for (int i = 0; i < blendshapes.Length - 1; i++) {
                if (blendshapes[i].value == 0 || blendshapes[i].index == blendshapes[i + 1].index) {
                    ArrayUtils.RemoveAt(ref blendshapes, i);
                    dirty = true;
                    i--;
                }
            }
            if (blendshapes.Length > 0 && blendshapes[^1].value == 0) {
                ArrayUtils.RemoveAt(ref blendshapes, blendshapes.Length - 1);
                dirty = true;
            }
        }

        static void RemoveOverRange(ref ConstantKandraBlendshapes.ConstantBlendshape[] blendshapes, int count, ref bool dirty) {
            for (int i = blendshapes.Length - 1; i >= 0; i--) {
                if (blendshapes[i].index >= count) {
                    ArrayUtils.RemoveAt(ref blendshapes, i);
                    dirty = true;
                } else {
                    return;
                }
            }
        }
    }
}