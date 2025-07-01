using System;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace Awaken.Kandra.AnimationPostProcessing {
    [CreateAssetMenu(menuName = "TG/Assets/Anims/Animation Post Processing")]
    public class AnimationPostProcessingPreset : ScriptableObject {
        [TableList(IsReadOnly = true)] public Transformation[] transformations = Array.Empty<Transformation>();
        
        [Serializable, StructLayout(LayoutKind.Explicit)]
        public unsafe struct Transformation {
            [FieldOffset(0), NonSerialized] public FixedString32Bytes bone;
            [FieldOffset(0), HideInInspector, SerializeField] fixed byte boneBytes[32];
            [FieldOffset(32), HideLabel, VerticalGroup("Position", order: 1)] public Vector3 position;
            [FieldOffset(44), HideLabel, VerticalGroup("Scale", order: 1)] public Vector3 scale;
            
            [HideLabel, VerticalGroup("Bone"), ShowInInspector] string BoneName => bone.ToString();
        }
    }
}