using System;
using Animancer;
using Awaken.TG.Code.Utility;

namespace Awaken.TG.Main.Locations.Pets {
    [Serializable]
    public class ARPetAnimationEntry {
        public ARPetAnimancer.State state;
        public ClipTransition[] clips = Array.Empty<ClipTransition>();
        
        public ClipTransition GetClip() {
            return clips.Length > 0 ? RandomUtil.UniformSelect(clips) : null;
        }
    }
}