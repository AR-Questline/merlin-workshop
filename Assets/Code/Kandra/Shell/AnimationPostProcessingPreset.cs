using System;
using Unity.Collections;
using UnityEngine;

namespace Awaken.Kandra.AnimationPostProcess {
    public class AnimationPostProcessingPreset : ScriptableObject {
        public Transformation[] transformations = Array.Empty<Transformation>();

        public struct Transformation {
            public FixedString32Bytes bone;
            public Vector3 position;
            public Vector3 scale;
            public string BoneName;
        }
    }
}