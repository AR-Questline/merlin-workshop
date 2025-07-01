using System;
using Animancer;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    [CreateAssetMenu(menuName = "NpcData/Pet/Pet Animations")]
    public class ARPetAnimationMapping : ScriptableObject {
        public ClipTransition[] idleClips = Array.Empty<ClipTransition>();
        public ClipTransition[] tauntClips = Array.Empty<ClipTransition>();
        public ClipTransition petClip;
        public MixerTransition2D movementMixer;
        public float movementMixerFollowSpeed = 4.0f;
    }
}