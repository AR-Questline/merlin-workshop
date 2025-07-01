using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra.AnimationPostProcessing {
    public class AnimationPostProcessing : MonoBehaviour {
        [SerializeField] Entry[] entries = Array.Empty<Entry>();
        
        [NonSerialized, ShowInInspector, ReadOnly] Entry[] _additionalEntries = Array.Empty<Entry>();

        [NonSerialized, ShowInInspector, ReadOnly] public Transform[] transforms = Array.Empty<Transform>();
        [NonSerialized, ShowInInspector, ReadOnly] public Vector3[] positions = Array.Empty<Vector3>();
        [NonSerialized, ShowInInspector, ReadOnly] public Vector3[] scales = Array.Empty<Vector3>();
        [NonSerialized, ShowInInspector, ReadOnly] public int[] batchStartIndex;
        
        void Awake() {
            RecalculateData();
        }

        void OnEnable() {
            AnimationPostProcessingService.Register(this);
        }

        void OnDisable() {
            AnimationPostProcessingService.Unregister(this);
        }
        
        public void ChangeAdditionalEntries(Entry[] entries) {
            _additionalEntries = entries;
            Refresh();
        }
        
        [Button]
        public void Refresh() {
            if (enabled && gameObject.activeInHierarchy) {
                OnDisable();
                RecalculateData();
                OnEnable();
            } else {
                RecalculateData();
            }
        }

        void RecalculateData() {
            var rig = GetComponent<KandraRig>();
            if (rig == null) {
                Log.Important?.Error($"AnimationPostProcessing without KandraRig on {gameObject}", gameObject, LogOption.NoStacktrace);
                return;
            }
            var datas = BuildIntermediateData(rig);
            ApplyIntermediateData(datas);
        }

        IntermediateBoneData[] BuildIntermediateData(KandraRig rig) {
            var datas = new IntermediateBoneData[rig.bones.Length];
            foreach (ref readonly var entry in entries.RefIterator()) {
                Append(entry);
            }
            foreach (ref readonly var entry in _additionalEntries.RefIterator()) {
                Append(entry);
            }
            return datas;

            void Append(in Entry entry) {
                if (entry.preset == null) {
                    return;
                }
                foreach (ref readonly var transformation in entry.preset.transformations.RefIterator()) {
                    var index = Array.IndexOf(rig.boneNames, transformation.bone);
                    if (index == -1) {
                        Log.Important?.Error($"Bone {transformation.bone} not found in KandraRig on {gameObject}", gameObject, LogOption.NoStacktrace);
                        continue;
                    }
                    ref var data = ref datas[index];
                    if (data.transform is null) {
                        data.transform = rig.bones[index];
                        data.position = data.transform.localPosition;
                        data.scale = data.transform.localScale;
                    }
                    data.position += transformation.position * entry.weight;
                    data.scale *= math.pow(transformation.scale, entry.weight);
                }
            }
        }

        void ApplyIntermediateData(IntermediateBoneData[] datas) {
            int count = 0;
            foreach (ref var data in datas.RefIterator()) {
                if (data.transform is not null) {
                    count++;
                }
            }
            const int BatchSize = AnimationPostProcessingService.BatchSize;
            int batchCount = Mathf.CeilToInt(count / (float)BatchSize);
            count = batchCount * BatchSize;
            
            transforms = new Transform[count];
            positions = new Vector3[count];
            scales = new Vector3[count];
            batchStartIndex = new int[batchCount];

            int index = 0;
            foreach (ref var data in datas.RefIterator()) {
                if (data.transform is null) {
                    continue;
                }
                transforms[index] = data.transform;
                positions[index] = data.position;
                scales[index] = data.scale;
                index++;
            }
            
            Array.Fill(batchStartIndex, -1);
        }

        [Serializable]
        public struct Entry {
            [HorizontalGroup, HideLabel] public AnimationPostProcessingPreset preset;
            [HorizontalGroup, HideLabel, Range(-1, 1)] public float weight;
            
            public Entry(AnimationPostProcessingPreset preset, float weight = 1) {
                this.preset = preset;
                this.weight = weight;
            }
        }

        struct IntermediateBoneData {
            public Transform transform;
            public Vector3 position;
            public Vector3 scale;            
        }
    }
}