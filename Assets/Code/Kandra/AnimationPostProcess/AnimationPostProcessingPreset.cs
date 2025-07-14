using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace Awaken.Kandra.AnimationPostProcess {
    [CreateAssetMenu(menuName = "TG/Assets/Anims/Animation Post Processing")]
    public class AnimationPostProcessingPreset : ScriptableObject {
        public Transformation[] transformations = Array.Empty<Transformation>();

        [Serializable, StructLayout(LayoutKind.Explicit)]
        public unsafe struct Transformation {
            [FieldOffset(0), NonSerialized] public FixedString32Bytes bone;
            [FieldOffset(0), HideInInspector, SerializeField] fixed byte boneBytes[32];
            [FieldOffset(32)] public Vector3 position;
            [FieldOffset(44)] public Vector3 scale;

            public string BoneName => bone.ToString();
        }
    }
}