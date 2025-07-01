using FMOD;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    public struct FlockSoundsData : ISharedComponentData {
        public float2 flyingOrRestingSoundPlayDelayMinMax;
        public FMOD.GUID groupFlyingEventGuid;
        public FMOD.GUID groupRestingEventGuid;
        public FMOD.GUID groupTakeOffEventGuid;
        public FMOD.GUID restingSoundGuid;
        public FMOD.GUID flyingSoundGuid;
        public FMOD.GUID takeOffEventGuid;
        public FMOD.GUID landEventGuid;

        public FlockSoundsData(float2 flyingOrRestingSoundPlayDelayMinMax, GUID groupFlyingEventGuid, GUID groupRestingEventGuid, GUID groupTakeOffEventGuid, GUID restingSoundGuid, GUID flyingSoundGuid, GUID takeOffEventGuid, GUID landEventGuid) {
            this.flyingOrRestingSoundPlayDelayMinMax = flyingOrRestingSoundPlayDelayMinMax;
            this.groupFlyingEventGuid = groupFlyingEventGuid;
            this.groupRestingEventGuid = groupRestingEventGuid;
            this.groupTakeOffEventGuid = groupTakeOffEventGuid;
            this.restingSoundGuid = restingSoundGuid;
            this.flyingSoundGuid = flyingSoundGuid;
            this.takeOffEventGuid = takeOffEventGuid;
            this.landEventGuid = landEventGuid;
        }
    }
}