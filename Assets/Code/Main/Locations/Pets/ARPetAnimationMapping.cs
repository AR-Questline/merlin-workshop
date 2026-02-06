using System;
using Animancer;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    [CreateAssetMenu(menuName = "NpcData/Pet/Pet Animations")]
    public class ARPetAnimationMapping : ScriptableObject {
        public ARPetAnimationEntry[] entries = Array.Empty<ARPetAnimationEntry>();
        public MixerTransition2D movementMixer;
        public float movementMixerFollowSpeed = 4.0f;
        
        public ITransition GetAnimation(ARPetAnimancer.State state) {
            if (state == ARPetAnimancer.State.Movement) {
                return movementMixer;
            }
            
            foreach (var entry in entries) {
                if (entry.state == state) {
                    return entry.GetClip();
                }
            }
            
            return null;
        }
    }
}