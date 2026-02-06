using System;
using UnityEngine;

namespace Awaken.Kandra.AnimationPostProcess {
    public class AnimationPostProcessing : MonoBehaviour {
        public Transform[] transforms = Array.Empty<Transform>();
        public Vector3[] positions = Array.Empty<Vector3>();
        public Vector3[] scales = Array.Empty<Vector3>();
        public int[] batchStartIndex = Array.Empty<int>();
        private Entry[] entries;
        private Entry[] _additionalEntries;

        public void ChangeAdditionalEntries(Entry[] entries) {
            throw new NotImplementedException();
        }

        public void Refresh() {
            throw new NotImplementedException();
        }

        public struct Entry {
            public AnimationPostProcessingPreset preset;
            public float weight;

            public Entry(AnimationPostProcessingPreset preset, float weight = 1) {
                throw new NotImplementedException();
            }
        }

        public struct EditorAccess {
            public static ref readonly Entry[] Entries(AnimationPostProcessing pp) => ref pp.entries;

            public static ref readonly Entry[] AdditionalEntries(AnimationPostProcessing pp) =>
                ref pp._additionalEntries;
        }
    }
}