using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.Kandra;
using Awaken.TG.Code.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features.Config {
    [Serializable]
    public class BlendShapeGroupSO : ScriptableObject {
        [SerializeField] BlendShapeGroup[] groups = Array.Empty<BlendShapeGroup>();
        [SerializeField, Sirenix.OdinInspector.ReadOnly] int[] blendShapesToSkip = Array.Empty<int>();

        public BlendShape[] CollectBlendshapes() {
            var blendshapesCount = 0;
            foreach (BlendShapeGroup group in groups) {
                if (group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.One)) {
                    ++blendshapesCount;
                } else if (group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.Multiple)) {
                    blendshapesCount += math.min(group.quantityToRandomize, group.blendShapes.Length);
                }
            }

            var result = new BlendShape[blendshapesCount];
            var resultIndex = 0;
            foreach (BlendShapeGroup group in groups) {
                if (group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.One)) {
                    var continuousValue = group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.ContinuousValue);
                    var shape = group.blendShapes[RandomUtil.UniformInt(0, group.blendShapes.Length - 1)];
                    result[resultIndex++] = new(shape, Weight(continuousValue));
                } else if (group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.Multiple)) {
                    var continuousValue = group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.ContinuousValue);
                    if (group.quantityToRandomize >= group.blendShapes.Length) {
                        foreach (var shape in group.blendShapes) {
                            result[resultIndex++] = new(shape, Weight(continuousValue));
                        }
                    } else {
                        var alreadySelected = new UnsafeBitmask((uint)group.blendShapes.Length, ARAlloc.Temp);
                        var maxBlendShapesIndex = (uint)(group.blendShapes.Length - 1);
                        for (var i = 0; i < group.quantityToRandomize; i++) {
                            uint selected;
                            do {
                                selected = RandomUtil.UniformUInt(0, maxBlendShapesIndex);
                            }
                            while (alreadySelected[selected]);
                            alreadySelected.Up(selected);

                            result[resultIndex++] = new(group.blendShapes[selected], Weight(continuousValue));
                        }
                        alreadySelected.Dispose();
                    }
                }
            }

            return result;
        }
        
        public void ApplyBlendshapes(KandraRenderer kandraRenderer) {
            foreach (BlendShapeGroup group in groups) {
                if (group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.One)) {
                    var continuousValue = group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.ContinuousValue);
                    var shape = group.blendShapes[RandomUtil.UniformInt(0, group.blendShapes.Length - 1)];
                    ApplyBlendshape(kandraRenderer, shape, continuousValue);
                } else if (group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.Multiple)) {
                    var continuousValue = group.mode.HasFlagFast(BlendShapeGroup.BlendshapesRandomizerMode.ContinuousValue);
                    if (group.quantityToRandomize >= group.blendShapes.Length) {
                        foreach (var shape in group.blendShapes) {
                            ApplyBlendshape(kandraRenderer, shape, continuousValue);
                        }
                    } else {
                        var alreadySelected = new UnsafeBitmask((uint)group.blendShapes.Length, ARAlloc.Temp);
                        var maxBlendShapesIndex = (uint)(group.blendShapes.Length - 1);
                        for (var i = 0; i < group.quantityToRandomize; i++) {
                            uint selected;
                            do {
                                selected = RandomUtil.UniformUInt(0, maxBlendShapesIndex);
                            }
                            while (alreadySelected[selected]);
                            alreadySelected.Up(selected);

                            ApplyBlendshape(kandraRenderer, group.blendShapes[selected], continuousValue);
                        }
                        alreadySelected.Dispose();
                    }
                }
            }
        }

        public bool ShouldSkipBlendshape(int index) {
            return blendShapesToSkip.Contains(index);
        }

        void ApplyBlendshape(KandraRenderer kandraRenderer, string shape, bool continuousValue) {
            var index = kandraRenderer.GetBlendshapeIndex(shape);
            if (index >= 0) {
                var weight = Weight(continuousValue);
                kandraRenderer.SetBlendshapeWeight((ushort)index, weight);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float Weight(bool continuousValue) {
            return continuousValue ? RandomUtil.UniformFloat(0.3f, 1f) : 1f;
        }

#if UNITY_EDITOR
        public readonly struct EditorAccess {
            readonly BlendShapeGroupSO _target;

            public EditorAccess(BlendShapeGroupSO target) {
                _target = target;
            }

            public void ResetFromSkinnedMeshRenderer(KandraRenderer kandraRenderer) {
                var newBlendshapes = new OnDemandCache<string, List<string>>(_ => new List<string>(4));
                var newBlendshapesToSkip = new List<int>();

                for (ushort i = 0; i < kandraRenderer.BlendshapesCount; i++) {
                    var bsName = kandraRenderer.GetBlendshapeName(i);
                    var lastIndexOfDelimiter = bsName.LastIndexOf('_');
                    if (lastIndexOfDelimiter < 1) {
                        newBlendshapesToSkip.Add(i);
                        continue;
                    }

                    var bsCategory = bsName[..lastIndexOfDelimiter];
                    var shapes = newBlendshapes[bsCategory];
                    shapes.Add(bsName);
                }

                var groups = new BlendShapeGroup[newBlendshapes.Count];
                var groupIndex = 0;
                foreach (var newBlendshape in newBlendshapes) {
                    var group = new BlendShapeGroup {
                        groupCategory = newBlendshape.Key,
                        blendShapes = newBlendshape.Value.ToArray(),
                        mode = BlendShapeGroup.BlendshapesRandomizerMode.One,
                        quantityToRandomize = 2,
                    };
                    groups[groupIndex++] = group;
                }

                _target.groups = groups;
                _target.blendShapesToSkip = newBlendshapesToSkip.ToArray();

                UnityEditor.EditorUtility.SetDirty(_target);
            }
        }
#endif
    }
}