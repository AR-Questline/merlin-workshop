using System;
using FMOD;

namespace Awaken.ECS.Critters.Components {
    [Serializable]
    public struct CritterSoundsGuids {
        public FMOD.GUID idleSoundGuid;
        public FMOD.GUID movementSoundGuid;
        
        public CritterSoundsGuids(GUID idleSoundGuid, GUID movementSoundGuid) {
            this.idleSoundGuid = idleSoundGuid;
            this.movementSoundGuid = movementSoundGuid;
        }
    }
}