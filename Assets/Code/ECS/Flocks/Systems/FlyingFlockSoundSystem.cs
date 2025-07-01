using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.Utility.Collections;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup))]
    [UpdateBefore(typeof(FlockEntityMovementSystem)), UpdateAfter(typeof(FlockEntityAvoidanceDataSetSystem))]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlyingFlockSoundSystem : SystemBase {
        const float UpdateSmoothDeltaTimeFrequency = 30 * 4;
        const int FlockEntitiesCountToPlayGroupSound = 3;
        const int FlyingSoundIndex = 0;
        const int RestingSoundIndex = 1;
        const int TakeOffSoundIndex = 2;
        const int NewGroupSoundsPreAllocCount = 2;
        public int FlyingBirdsCount { get; private set; }
        public int RestingBirdsCount { get; private set; }
        public int TakingOffBirdsCount { get; private set; }

        EntityTypeHandle _entityTypeHandle;
        SharedComponentTypeHandle<FlockGroupEntity> _flockGroupEntityHandle;
        SharedComponentTypeHandle<FlockSoundsData> _flockSoundDataHandle;
        ComponentTypeHandle<FlyingFlockEntityState> _flyingFlockEntityStateHandle;
        ComponentTypeHandle<DrakeVisualEntitiesTransform> _flockEntityTransformHandle;

        NativeHashMap<int2, EventInstanceWithSoundPosDataIndex> _soundIndexToGroupSoundData;
        NativeList<SoundIndexWithSoundPosData> _soundIndexWithPositionData;
        float _smoothedDeltaTimeUpdatedTime;
        float _smoothedDeltaTime;
        float _groupFlyingSoundLastPlayTime;
        float _groupTakeOffSoundLastPlayTime;
        float _groupRestingSoundLastPlayTime;
        EntityQuery _query;

        protected override void OnCreate() {
            base.OnCreate();
            _query = SystemAPI.QueryBuilder().WithPresent<FlyingFlockEntityState, FlockSoundsData, DrakeVisualEntitiesTransform>().WithNone<CulledEntityTag>().Build();

            _entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            _flockGroupEntityHandle = SystemAPI.GetSharedComponentTypeHandle<FlockGroupEntity>();
            _flockSoundDataHandle = SystemAPI.GetSharedComponentTypeHandle<FlockSoundsData>();
            _flyingFlockEntityStateHandle = SystemAPI.GetComponentTypeHandle<FlyingFlockEntityState>();
            _flockEntityTransformHandle = SystemAPI.GetComponentTypeHandle<DrakeVisualEntitiesTransform>();

            _soundIndexToGroupSoundData = new(2, ARAlloc.Persistent);
            _soundIndexWithPositionData = new(2, ARAlloc.Persistent);
        }

        protected override unsafe void OnUpdate() {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            bool needsUpdateSmoothDeltaTime = currentTime - _smoothedDeltaTimeUpdatedTime > UpdateSmoothDeltaTimeFrequency;
            _smoothedDeltaTimeUpdatedTime = math.select(_smoothedDeltaTimeUpdatedTime, currentTime, needsUpdateSmoothDeltaTime);
            _smoothedDeltaTime = math.select(_smoothedDeltaTime, deltaTime, needsUpdateSmoothDeltaTime | (deltaTime != 0 & _smoothedDeltaTime == 0));

            if (_smoothedDeltaTime == 0) {
                return;
            }

            int currentFrame = UnityEngine.Time.frameCount;
            
            _entityTypeHandle.Update(this);
            _flockGroupEntityHandle.Update(this);
            _flockSoundDataHandle.Update(this);
            _flyingFlockEntityStateHandle.Update(this);
            _flockEntityTransformHandle.Update(this);

            var newSoundEventsData = new UnsafeList<(GUID soundGuid, int2 soundIndex)>(NewGroupSoundsPreAllocCount, ARAlloc.Temp);
            
            for (int i = 0; i < _soundIndexWithPositionData.Length; i++) {
                var data = _soundIndexWithPositionData[i];
                data.positionData = default;
                _soundIndexWithPositionData[i] = data;
            }

            var chunks = _query.ToArchetypeChunkArray(ARAlloc.Temp);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++) {
                var chunk = chunks[chunkIndex];
                var soundsData = chunk.GetSharedComponent(_flockSoundDataHandle);
                var entities = chunk.GetEntityDataPtrRO(_entityTypeHandle);
                var states = chunk.GetComponentDataPtrRO(ref _flyingFlockEntityStateHandle);
                var transforms = chunk.GetComponentDataPtrRO(ref _flockEntityTransformHandle);

                int chunkEntitiesCount = chunk.Count;
                float3 groupFlyingSumPosition = 0, groupRestingSumPosition = 0, groupTakeOffSumPosition = 0;
                int groupFlyingCount = 0, groupRestingCount = 0, groupTakeOffCount = 0;

                for (int i = 0; i < chunkEntitiesCount; i++) {
                    var entity = entities[i];
                    var state = states[i];
                    var transform = transforms[i];

                    var soundStates = state.value &
                                      (FlyingFlockEntityState.State.Flapping | FlyingFlockEntityState.State.Soaring | FlyingFlockEntityState.State.Resting |
                                       FlyingFlockEntityState.State.TakingOff | FlyingFlockEntityState.State.Landing);
                    // Taking off and landing can be enabled simultaneously with other states, so for switch to work, they need to be masked if any of them is enabled 
                    soundStates = ((soundStates & FlyingFlockEntityState.State.TakingOff) != 0) | ((soundStates & FlyingFlockEntityState.State.Landing) != 0) ?
                        soundStates & (FlyingFlockEntityState.State.TakingOff | FlyingFlockEntityState.State.Landing) :
                        soundStates;

                    var entityStableHash = math.hash(new int2(entity.Index, entity.Version));
                    var soundFrequencyMinFrames = (int)(soundsData.flyingOrRestingSoundPlayDelayMinMax.x / _smoothedDeltaTime);
                    var soundFrequencyMaxFrames = (int)(soundsData.flyingOrRestingSoundPlayDelayMinMax.y / _smoothedDeltaTime);
                    bool playEntityStateSound = StatelessRandom.IsRandomOneFrameStateEnabledInCurrentFrame(
                        new int2(soundFrequencyMinFrames, soundFrequencyMaxFrames), currentFrame, entityStableHash);

                    switch (soundStates) {
                        case FlyingFlockEntityState.State.Flapping:
                        case FlyingFlockEntityState.State.Soaring:
                            groupFlyingCount++;
                            groupFlyingSumPosition += transform.position;
                            if (playEntityStateSound && soundsData.flyingSoundGuid != default) {
                                //RuntimeManager.PlayOneShot(new EventReference(){ Guid = soundsData.flyingSoundGuid}, transform.position);
                            }
                            break;
                        case FlyingFlockEntityState.State.Resting:
                            groupRestingCount++;
                            groupRestingSumPosition += transform.position;

                            if (playEntityStateSound && soundsData.restingSoundGuid != default) {
                                //RuntimeManager.PlayOneShot(new EventReference(){ Guid = soundsData.restingSoundGuid}, transform.position);
                            }
                            break;
                        case FlyingFlockEntityState.State.TakingOff:
                            groupTakeOffSumPosition += transform.position;
                            groupTakeOffCount++;
                            if (soundsData.takeOffEventGuid != default) {
                                //RuntimeManager.PlayOneShot(new EventReference(){ Guid = soundsData.takeOffEventGuid}, transform.position);
                            }
                            break;
                        case FlyingFlockEntityState.State.Landing:
                            if (soundsData.landEventGuid != default) {
                                //RuntimeManager.PlayOneShot(new EventReference(){ Guid = soundsData.landEventGuid}, transform.position);
                            }
                            break;
                    }
                }

                var flockGroupEntityIndex = chunk.GetSharedComponentIndex(_flockGroupEntityHandle);

                if (groupFlyingCount > 0) {
                    ProcessChunkGroupSoundData(
                        soundsData.groupFlyingEventGuid, flockGroupEntityIndex, FlyingSoundIndex, groupFlyingSumPosition, groupFlyingCount, ref newSoundEventsData);
                }

                if (groupRestingCount > 0) {
                    ProcessChunkGroupSoundData(
                        soundsData.groupRestingEventGuid, flockGroupEntityIndex, RestingSoundIndex, groupRestingSumPosition, groupRestingCount, ref newSoundEventsData);
                }

                if (groupTakeOffCount > 0) {
                    ProcessChunkGroupSoundData(
                        soundsData.groupTakeOffEventGuid, flockGroupEntityIndex, TakeOffSoundIndex, groupTakeOffSumPosition, groupTakeOffCount, ref newSoundEventsData);
                }
            }

            chunks.Dispose();

            for (int i = 0; i < newSoundEventsData.m_length; i++) {
                var (soundGuid, soundIndex) = newSoundEventsData[i];
                var groupSoundData = _soundIndexToGroupSoundData[soundIndex];
                var positionData = _soundIndexWithPositionData[groupSoundData.soundPosDataIndex].positionData;
                if (positionData.entitiesCount < FlockEntitiesCountToPlayGroupSound) {
                    continue;
                }
                // if (RuntimeManager.TryCreateInstance(soundGuid, out var eventInstance, default)) {
                //     eventInstance.start();
                //     groupSoundData.eventInstance = eventInstance;
                //     _soundIndexToGroupSoundData[soundIndex] = groupSoundData;
                // }
            }

            newSoundEventsData.Dispose();

            FlyingBirdsCount = 0;
            RestingBirdsCount = 0;
            TakingOffBirdsCount = 0;
            for (int positionDataIndex = 0; positionDataIndex < _soundIndexWithPositionData.Length; positionDataIndex++) {
                var (soundIndex, positionData) = _soundIndexWithPositionData[positionDataIndex];
                var eventInstance = _soundIndexToGroupSoundData[soundIndex].eventInstance;
                // if (eventInstance.isValid() == false) {
                //     continue;
                // }
                // if (positionData.entitiesCount >= FlockEntitiesCountToPlayGroupSound) {
                //     switch (soundIndex.y) {
                //         case FlyingSoundIndex:
                //             FlyingBirdsCount += positionData.entitiesCount;
                //             break;
                //         case RestingSoundIndex:
                //             RestingBirdsCount += positionData.entitiesCount;
                //             break;
                //         case TakeOffSoundIndex:
                //             TakingOffBirdsCount += positionData.entitiesCount;
                //             break;
                //     }
                //
                //     eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(positionData.position));
                // } else {
                //     if (eventInstance.hasHandle()) {
                //         eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                //         RuntimeManager.ReleaseInstance(eventInstance);
                //     }
                //     var lastPositionDataIndex = _soundIndexWithPositionData.Length - 1;
                //     var lastPositionData = _soundIndexWithPositionData[lastPositionDataIndex];
                //     _soundIndexWithPositionData.RemoveAtSwapBack(positionDataIndex);
                //     _soundIndexToGroupSoundData.Remove(soundIndex);
                //     if (lastPositionDataIndex != positionDataIndex) {
                //         var lastPositionDataFlockGroupSoundData = _soundIndexToGroupSoundData[lastPositionData.soundIndex];
                //         lastPositionDataFlockGroupSoundData.soundPosDataIndex = positionDataIndex;
                //         _soundIndexToGroupSoundData[lastPositionData.soundIndex] = lastPositionDataFlockGroupSoundData;
                //     }
                // }
            }

            newSoundEventsData.Dispose();
        }

        void ProcessChunkGroupSoundData(GUID soundEventGuid, int flockGroupIndex, int soundTypeIndex, float3 groupActiveEntitiesSumPosition, int groupActiveEntitiesCount,
            ref UnsafeList<(GUID soundGuid, int2 soundIndex)> newSoundEventsData) {
            var soundIndex = new int2(flockGroupIndex, soundTypeIndex);
            if (_soundIndexToGroupSoundData.TryGetValue(soundIndex, out var groupSoundData) == false) {
                newSoundEventsData.Add((soundEventGuid, soundIndex));
                var averagePosition = groupActiveEntitiesSumPosition / groupActiveEntitiesCount;
                var soundPositionDataIndex = _soundIndexWithPositionData.Length;
                _soundIndexWithPositionData.Add(new(soundIndex, new(averagePosition, groupActiveEntitiesCount)));
                _soundIndexToGroupSoundData.Add(soundIndex, new(default, soundPositionDataIndex));
            } else {
                var prevPositionData = _soundIndexWithPositionData[groupSoundData.soundPosDataIndex].positionData;
                var prevSumPosition = prevPositionData.position * prevPositionData.entitiesCount;
                var newSumPosition = prevSumPosition + groupActiveEntitiesSumPosition;
                var newEntitiesCount = prevPositionData.entitiesCount + groupActiveEntitiesCount;
                var newAveragePosition = newSumPosition / newEntitiesCount;
                var newPositionData = new SoundPositionData(newAveragePosition, newEntitiesCount);
                _soundIndexWithPositionData[groupSoundData.soundPosDataIndex] = new(soundIndex, newPositionData);
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            _soundIndexToGroupSoundData.Dispose();
            _soundIndexWithPositionData.Dispose();
        }

        struct EventInstanceWithSoundPosDataIndex {
            public EventInstance eventInstance;
            public int soundPosDataIndex;

            public EventInstanceWithSoundPosDataIndex(EventInstance eventInstance, int soundPosDataIndex) {
                this.eventInstance = eventInstance;
                this.soundPosDataIndex = soundPosDataIndex;
            }
        }

        struct SoundIndexWithSoundPosData {
            public int2 soundIndex;
            public SoundPositionData positionData;

            public SoundIndexWithSoundPosData(int2 soundIndex, SoundPositionData positionData) {
                this.soundIndex = soundIndex;
                this.positionData = positionData;
            }

            public void Deconstruct(out int2 soundIndex, out SoundPositionData positionData) {
                soundIndex = this.soundIndex;
                positionData = this.positionData;
            }
        }

        struct SoundPositionData {
            public float3 position;
            public int entitiesCount;

            public SoundPositionData(float3 position, int entitiesCount) {
                this.position = position;
                this.entitiesCount = entitiesCount;
            }
        }
    }
}