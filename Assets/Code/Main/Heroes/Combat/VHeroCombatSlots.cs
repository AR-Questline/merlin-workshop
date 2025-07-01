using System.Threading.Tasks;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [UsesPrefab("Hero/VHeroCombatSlots")]
    public class VHeroCombatSlots : View<HeroCombatSlots> {
        public const float CombatSlotOffset = 2f;
        public const float FirstLineCombatSlotOffset = CombatSlotOffset + FirstLineOffsetAdditionalRange;
        const float FirstLineOffsetAdditionalRange = 0.5f;
        const float SlotsInCircleMultiplier = 1.5f;
        const float MaxCombatSlotOffsetSqr = (CombatSlotOffset * 2) * (CombatSlotOffset * 2);
        const float HeroVelocityMultiplier = 0.5f;
        const float MinBackwardsVelocity = -1.25f;
        const float LastLineAdditionalOffset = 0.5f;

        [SerializeField] int midRangeSlotsCount = 164;
        [SerializeField, Range(0.5f, 2f)] float fistFightSlotsOffset = 1;
        [SerializeField] int slotsForFistFight = 8;
        [SerializeField] int slotsInFirstCircle = 12;
        [SerializeField] int slotsInLastCircle = 50;
        public float maxSlotPositionDiff = 0.3f;

        public int SecondLineSlotsStartIndex { get; private set; }
        int AllSlotsCount => midRangeSlotsCount + slotsForFistFight;

        NativeArray<float3> _slotsNewPositions;
        UnsafeBitmask _slotsNewValidOnNavmeshStatuses;
        UnsafeBitmask _slotsNewInFovStatuses;
        UnsafeBitmask _slotsNewHeroReachableStatuses;

        Vector3 _heroBackwardsVelocity;
        bool _isUpdatingSlotsPositions, _slotsUpdatesCompleted;
        System.Action _updateSlotsPositionsAndStatusesDelegate;
        Task _updateSlotsTask;

        protected override void OnInitialize() {
            SecondLineSlotsStartIndex = AllSlotsCount - slotsInLastCircle;
            _updateSlotsPositionsAndStatusesDelegate = UpdateSlotsPositionsAndStatuses;
        }

        void Update() {
            transform.position = Target.ParentModel.Coords;
            if (!Target.CanUpdate) {
                return;
            }

            if (_slotsUpdatesCompleted) {
                if (_slotsNewHeroReachableStatuses.IsCreated) {
                    AssignUpdatedSlotInfoToSlots();
                }
                _isUpdatingSlotsPositions = false;
            }

            if (_isUpdatingSlotsPositions == false) {
                _isUpdatingSlotsPositions = true;
                _slotsUpdatesCompleted = false;
                UpdateHeroBackwardsVelocity();
                if (CanUpdateSlotsInfo() == false) {
                    if (_slotsNewHeroReachableStatuses.IsCreated) {
                        _slotsNewPositions.Dispose();
                        _slotsNewValidOnNavmeshStatuses.Dispose();
                        _slotsNewInFovStatuses.Dispose();
                        _slotsNewHeroReachableStatuses.Dispose();
                    }
                    _slotsUpdatesCompleted = true;
                } else {
                    if (_slotsNewPositions.IsCreated == false) {
                        var slotsCount = Target.SlotsCount;
                        _slotsNewPositions = new NativeArray<float3>(slotsCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                        _slotsNewValidOnNavmeshStatuses = new UnsafeBitmask((uint)slotsCount, ARAlloc.Persistent);
                        _slotsNewInFovStatuses = new UnsafeBitmask((uint)slotsCount, ARAlloc.Persistent);
                        _slotsNewHeroReachableStatuses = new UnsafeBitmask((uint)slotsCount, ARAlloc.Persistent);
                    }

                    _updateSlotsTask = Task.Run(_updateSlotsPositionsAndStatusesDelegate);
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (Application.isPlaying) {
                Target.DrawPlaymodeGizmos();
            } else {
                Gizmos.color = Color.green;
                var slotsLocalPositions = GenerateSlotsLocalPositions(ARAlloc.Temp);
                var heroPos = transform.position;
                for (int i = 0; i < slotsLocalPositions.Length; i++) {
                    var slotLocalPos = slotsLocalPositions[i];
                    var slotWorldPos = heroPos + new Vector3(slotLocalPos.x, 0, slotLocalPos.y);
                    Gizmos.DrawSphere(slotWorldPos, HeroCombatSlots.GizmosSphereSize);
                }

                slotsLocalPositions.Dispose();
                Gizmos.color = Color.white;
            }
        }
#endif

        public override Transform DetermineHost() => Services.Get<ViewHosting>().DefaultForHero();

        public NativeArray<float2> GenerateSlotsLocalPositions(Allocator allocator) {
            int allSlotsCount = AllSlotsCount;
            var slotsPositions = new NativeArray<float2>(allSlotsCount, allocator, NativeArrayOptions.UninitializedMemory);
            float currentAngle = 0;
            float angleStep = 360f / slotsForFistFight;
            int circleIndex = 0;
            int indexInCircle = 0;
            int slotsCountInCircle = slotsForFistFight;
            for (int i = 0; i < allSlotsCount; i++) {
                bool lastLine = false;

                if (indexInCircle == slotsCountInCircle) {
                    circleIndex++;
                    currentAngle = 0;
                    indexInCircle = 0;
                    int slotsRemaining = allSlotsCount - i;
                    if (circleIndex == 1) {
                        slotsCountInCircle = slotsInFirstCircle;
                    } else if (slotsCountInCircle > slotsRemaining) {
                        slotsCountInCircle = slotsInLastCircle;
                        lastLine = true;
                    } else {
                        slotsCountInCircle = Mathf.CeilToInt(slotsCountInCircle * SlotsInCircleMultiplier);
                    }

                    angleStep = 360f / slotsCountInCircle;
                }

                indexInCircle++;
                float offset;
                if (circleIndex == 0) {
                    offset = fistFightSlotsOffset;
                } else {
                    offset = FirstLineOffsetAdditionalRange + CombatSlotOffset * circleIndex;
                    offset += lastLine ? LastLineAdditionalOffset : 0;
                }

                slotsPositions[i] = ((float3)(Quaternion.AngleAxis(currentAngle, Vector3.up) * (Vector3.forward * offset))).xz;
                currentAngle += angleStep;
            }

            return slotsPositions;
        }

        void UpdateHeroBackwardsVelocity() {
            Vector3 heroForward = Target.ParentModel.Forward();
            Vector3 horizontalVelocity = Target.ParentModel.HorizontalVelocity;
            float horizontalVelocityMagnitude = horizontalVelocity.magnitude;
            Vector3 horizontalVelocityNormalized = horizontalVelocity / horizontalVelocityMagnitude;
            float forwardDot = Vector3.Dot(horizontalVelocityNormalized, heroForward);
            if (forwardDot < 0) {
                _heroBackwardsVelocity = heroForward * Mathf.Clamp(forwardDot * horizontalVelocityMagnitude * HeroVelocityMultiplier, MinBackwardsVelocity, 0);
            } else {
                _heroBackwardsVelocity = Vector3.zero;
            }
        }

        void UpdateSlotsPositionsAndStatuses() {
            try {
                Target.GetUpdatedPositionsAndStatuses(_heroBackwardsVelocity, maxSlotPositionDiff, _slotsNewPositions, in _slotsNewValidOnNavmeshStatuses,
                    in _slotsNewInFovStatuses, in _slotsNewHeroReachableStatuses);
            } finally {
                _slotsUpdatesCompleted = true;
            }
        }

        bool CanUpdateSlotsInfo() {
            if (AstarPath.active == false) {
                return false;
            }

            var hero = Hero.Current;
            var heroClosestPointOnNavmesh = hero.ClosestPointOnNavmesh;
            return heroClosestPointOnNavmesh.node != null && math.distancesq(heroClosestPointOnNavmesh.position, hero.Coords) <= MaxCombatSlotOffsetSqr;
        }

        void AssignUpdatedSlotInfoToSlots() {
            Target.SetUpdatedPositionsAndStatuses(_slotsNewPositions, in _slotsNewValidOnNavmeshStatuses,
                in _slotsNewInFovStatuses, in _slotsNewHeroReachableStatuses);
        }

        protected override IBackgroundTask OnDiscard() {
            if (_updateSlotsTask != null) {
                _updateSlotsTask.Wait();
                _updateSlotsTask.Dispose();
            }
            
            if (_slotsNewPositions.IsCreated) {
                _slotsNewPositions.Dispose();
            }
            _slotsNewValidOnNavmeshStatuses.DisposeIfCreated();
            _slotsNewInFovStatuses.DisposeIfCreated();
            _slotsNewHeroReachableStatuses.DisposeIfCreated();
            return base.OnDiscard();
        }
    }
}