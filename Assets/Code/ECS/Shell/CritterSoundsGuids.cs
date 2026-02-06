using System;
using FMOD;

namespace Awaken.ECS.Critters.Components {
    [Serializable]
    public struct CritterSoundsGuids {
        public GUID idleSoundGuid;
        public GUID movementSoundGuid;

        public CritterSoundsGuids(GUID idleSoundGuid, GUID movementSoundGuid) {
            throw new NotImplementedException();
        }
    }
}