using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Pathfinding;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [SpawnsView(typeof(VHeroCombatSlots))]
    public partial class HeroCombatSlots : Element<Hero> {
        public const float GizmosSphereSize = 0.25f;

        /// <summary>
        /// Value lower than 1 means that enemy will only claim better position if better position is surrounded by
        /// enemies with significantly lower aggression scores than the enemy which want to claim better position  
        /// </summary>
        const float AggressionScoreThreshold = 0.9f;

        const float RangeMinDistanceMultiplier = 0.3f;
        const float RangeMaxDistanceMultiplier = 1.5f;
        const float MinimalNeededDistanceToDesiredPosChange = 0.1f;
        const float BlockingRange = 2.75f;
        const float BlockingRangeSq = BlockingRange * BlockingRange;

        const float MaxVerticalDistance = -10f;
        const float PositionUpdateInterval = 0.1f;
        const float MaxPositionUpdates = 5 * PositionUpdateInterval;
        const float MaxVerticalPositionUpdates = 10 * PositionUpdateInterval;

        static readonly NNConstraint Constraint = new() {
            constrainWalkability = true,
            constrainDistance = true,
        };

        public int SlotsCount => _slotsOriginalLocalPositions.Length;
        public sealed override bool IsNotSaved => true;
        
        [UnityEngine.Scripting.Preserve] public int MaxBookedNPCs { get; [UnityEngine.Scripting.Preserve] private set; }
        public bool CanUpdate { get; private set; }

        NativeArray<float2> _slotsOriginalLocalPositions;
        NativeArray<float3> _slotsPositions;
        NativeArray<(int index, float distanceSq)> _slotNearestOccupiedSlotIndexAndDistanceSqArr;
        NativeArray<float> _slotsOccupantsAggressionScores;
        UnsafeBitmask _slotsInFovStatuses;
        UnsafeBitmask _slotsOccupiedStatuses;
        UnsafeBitmask _slotsValidOnNavmeshStatuses;
        UnsafeBitmask _slotsHeroReachableStatuses;

        WeakModelRef<EnemyBaseClass>[] _slotsOccupants;
        (IEventListener listener0, IEventListener listener1)[] _slotsOccupantsOnDeathEventsListeners;
        Dictionary<WeakModelRef<EnemyBaseClass>, int> _occupantEnemyRefToSlotIndexMap;
        VHeroCombatSlots _vHeroCombatSlots;
        Action<DamageOutcome> _onOccupantDeathIAliveDelegate;
        Action<Model> _onOccupantDeathModelDelegate;

        protected override void OnInitialize() {
            ParentModel.ListenTo(GroundedEvents.TeleportRequested, _ => CanUpdate = false, this);
            ParentModel.ListenTo(GroundedEvents.AfterTeleported, _ => CanUpdate = true, this);
            _onOccupantDeathIAliveDelegate = OnOccupantDeath;
            _onOccupantDeathModelDelegate = OnOccupantDeath;
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            // After view spawned
            _vHeroCombatSlots = View<VHeroCombatSlots>();

            _slotsOriginalLocalPositions = _vHeroCombatSlots.GenerateSlotsLocalPositions(ARAlloc.Persistent);

            uint slotsCount = (uint)SlotsCount;

            _slotsPositions = new NativeArray<float3>((int)slotsCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
            float3 heroPos = Hero.Current.Coords;
            for (int i = 0; i < slotsCount; i++) {
                _slotsPositions[i] = heroPos + new float3(_slotsOriginalLocalPositions[i].x, 0, _slotsOriginalLocalPositions[i].y);
            }

            _slotsOccupants = new WeakModelRef<EnemyBaseClass>[slotsCount];
            _slotsOccupantsOnDeathEventsListeners = new (IEventListener listener0, IEventListener listener1)[slotsCount];

            _slotNearestOccupiedSlotIndexAndDistanceSqArr = new NativeArray<(int index, float distance)>((int)slotsCount, ARAlloc.Persistent);
            for (int i = 0; i < slotsCount; i++) {
                _slotNearestOccupiedSlotIndexAndDistanceSqArr[i] = (-1, float.PositiveInfinity);
            }

            _slotsOccupantsAggressionScores = new NativeArray<float>((int)slotsCount, ARAlloc.Persistent);

            for (int i = 0; i < slotsCount; i++) {
                _slotsOccupantsAggressionScores[i] = float.NegativeInfinity;
            }
            
            _slotsInFovStatuses = new UnsafeBitmask(slotsCount, ARAlloc.Persistent);

            _slotsOccupiedStatuses = new UnsafeBitmask(slotsCount, ARAlloc.Persistent);

            _slotsValidOnNavmeshStatuses = new UnsafeBitmask(slotsCount, ARAlloc.Persistent);

            _slotsHeroReachableStatuses = new UnsafeBitmask(slotsCount, ARAlloc.Persistent);

            _occupantEnemyRefToSlotIndexMap = new((int)slotsCount);
        }

        public int GetOccupiedSlotsCount() => (int)_slotsOccupiedStatuses.CountOnes();
        public bool HasReachablePathToHero(int slotIndex) => _slotsHeroReachableStatuses[(uint)slotIndex];
        public float3 GetSlotWorldPosition(int slotIndex) => _slotsPositions[slotIndex];

        public bool TryGetAndOccupyCombatSlot(EnemyBaseClass enemy, Vector3 desiredPosition, float range, out int slotIndex) {
            int ownedSlotIndex = _occupantEnemyRefToSlotIndexMap.GetValueOrDefault(enemy, -1);
            if (ownedSlotIndex != -1) {
                slotIndex = ownedSlotIndex;
                return true;
            }


            var desiredPositionInSlotsSpace = TransformWordPosToSlotsPos(desiredPosition, Hero.Current.Coords);
            if (TryGetClosestAvailableSlotIndex(enemy, desiredPositionInSlotsSpace, range, out slotIndex)) {
                SetSlotOccupied(slotIndex, enemy);
                return true;
            }
            
            return false;
        }

        public bool TryGetAndOccupyBetterCombatSlot(EnemyBaseClass enemy, Vector3 desiredPosition, float range, out int slotIndex) {
            var occupiedSlotIndex = _occupantEnemyRefToSlotIndexMap.GetValueOrDefault(enemy, -1);
            var desiredPositionInSlotsSpace = TransformWordPosToSlotsPos(desiredPosition, Hero.Current.Coords);
            slotIndex = GetBetterSlotIndex(enemy, occupiedSlotIndex, desiredPositionInSlotsSpace, range, MinimalNeededDistanceToDesiredPosChange);
            if (slotIndex == -1) {
                return false;
            }
            if (slotIndex != occupiedSlotIndex) {
                if (occupiedSlotIndex != -1) {
                    SetSlotNotOccupied(occupiedSlotIndex);
                }
                OccupySlotAndReleaseNeighbourSlots(enemy, slotIndex, range);
            }
            return true;
        }

        public void ReleaseCombatSlot(EnemyBaseClass enemy) {
            var occupiedSlotIndex = _occupantEnemyRefToSlotIndexMap.GetValueOrDefault(enemy, -1);
            if (occupiedSlotIndex == -1) {
                return;
            }

            SetSlotNotOccupied(enemy);
        }

        public void GetUpdatedPositionsAndStatuses(Vector3 heroVelocity, float maxSlotPositionDiff, NativeArray<float3> newPositions, in UnsafeBitmask newValidOnNavmeshStatuses,
            in UnsafeBitmask newInFovStatuses, in UnsafeBitmask newHeroReachableStatuses) {
            var hero = Hero.Current;
            var heroPosition = hero.Coords;
            var heroRotation = hero.Rotation;
            var heroClosestPointOnNavmeshNode = hero.ClosestPointOnNavmesh.node;
            int slotsCount = SlotsCount;
            for (uint i = 0; i < slotsCount; i++) {
                float2 slotPosLocal2d = _slotsOriginalLocalPositions[(int)i];
                var slotPosLocal = new Vector3(slotPosLocal2d.x, 0, slotPosLocal2d.y);
                GetSlotNewPositionOnNavmesh(heroClosestPointOnNavmeshNode, slotPosLocal, heroPosition, heroVelocity, maxSlotPositionDiff,
                    out var newPosition, out var isPositionValidOnNavmesh, out var isHeroReachableFromNewPosition);
                newPositions[(int)i] = newPosition;
                newValidOnNavmeshStatuses[i] = isPositionValidOnNavmesh;
                var isNewPositionInHeroFov = AIUtils.IsInHeroViewCone(AIUtils.HeroDotToTarget(newPosition, heroPosition, heroRotation));
                newInFovStatuses[i] = isNewPositionInHeroFov;
                newHeroReachableStatuses[i] = isHeroReachableFromNewPosition;
            }
        }

        public void SetUpdatedPositionsAndStatuses(NativeArray<float3> newPositions, in UnsafeBitmask newValidOnNavmeshStatuses,
            in UnsafeBitmask newInFovStatuses, in UnsafeBitmask newHeroReachableStatuses) {
            _slotsPositions.CopyFrom(newPositions);
            _slotsValidOnNavmeshStatuses.CopyFrom(newValidOnNavmeshStatuses);
            _slotsInFovStatuses.CopyFrom(newInFovStatuses);
            _slotsHeroReachableStatuses.CopyFrom(newHeroReachableStatuses);
            
            ReleaseSlotsIfOccupantChangedInFovStatusOrDied(newInFovStatuses);
        }

        public void UpdateEnemyAggressionScore(WeakModelRef<EnemyBaseClass> enemyRef, float aggressionScore) {
            if (_occupantEnemyRefToSlotIndexMap.TryGetValue(enemyRef, out var combatSlotIndex) == false) {
                return;
            }

            _slotsOccupantsAggressionScores[combatSlotIndex] = aggressionScore;
        }
        
        void ReleaseSlotsIfOccupantChangedInFovStatusOrDied(UnsafeBitmask newInFovStatuses) {
            UnsafeList<int> combatSlotsToRelease = default;
            foreach (var (occupant, slotIndex) in _occupantEnemyRefToSlotIndexMap) {
                // if enemy died or occupant in fov status no longer matches slot in fov status - release slot
                if(occupant.TryGet(out var enemy) == false || (enemy.IsInHeroFov != newInFovStatuses[(uint)slotIndex]))
                {
                    if (combatSlotsToRelease.IsCreated == false) {
                        combatSlotsToRelease = new UnsafeList<int>(2, ARAlloc.Temp);
                    }
                    combatSlotsToRelease.Add(slotIndex);
                }
            }
            if (combatSlotsToRelease.IsCreated) {
                for (int i = 0; i < combatSlotsToRelease.Length; i++) {
                    SetSlotNotOccupied(combatSlotsToRelease[i]);
                }
                combatSlotsToRelease.Dispose();
            }
        }

        void SetSlotOccupied(int updatedSlotIndex, EnemyBaseClass enemy) {
            _slotsOccupiedStatuses.Up((uint)updatedSlotIndex);

            _slotsOccupantsAggressionScores[updatedSlotIndex] = enemy.AggressionScore;

            _slotsOccupants[updatedSlotIndex] = enemy;

            _occupantEnemyRefToSlotIndexMap.Add(enemy, updatedSlotIndex);
            enemy.OwnedCombatSlotIndex = updatedSlotIndex;

            var listener0 = enemy.NpcElement.ListenTo(IAlive.Events.BeforeDeath, _onOccupantDeathIAliveDelegate);
            var listener1 = enemy.ListenTo(Model.Events.BeforeDiscarded, _onOccupantDeathModelDelegate);
            _slotsOccupantsOnDeathEventsListeners[updatedSlotIndex] = (listener0, listener1);

            var updatedSlotPosition = _slotsOriginalLocalPositions[updatedSlotIndex];
            int slotsCount = SlotsCount;
            for (int i = 0; i < slotsCount; i++) {
                var slotPos = _slotsOriginalLocalPositions[i];
                var (indexOfNearestOccupiedNeighbour, distanceSqToNearestOccupiedNeighbour) = _slotNearestOccupiedSlotIndexAndDistanceSqArr[i];
                var distanceSqToUpdatedSlot = math.distancesq(updatedSlotPosition, slotPos);

                if (((indexOfNearestOccupiedNeighbour == -1) | (distanceSqToUpdatedSlot < distanceSqToNearestOccupiedNeighbour)) & (i != updatedSlotIndex)) {
                    _slotNearestOccupiedSlotIndexAndDistanceSqArr[i] = (updatedSlotIndex, distanceSqToUpdatedSlot);
                }
            }

            RecalculateSlotNearestOccupiedNeighbour(updatedSlotIndex);
        }

        void SetSlotNotOccupied(EnemyBaseClass enemy) {
            if (_occupantEnemyRefToSlotIndexMap.TryGetValue(enemy, out var occupiedSlotIndex) == false) {
                Log.Important?.Error($"Enemy {enemy.ParentModel.DisplayName} is not in {nameof(_occupantEnemyRefToSlotIndexMap)} dictionary", enemy.ParentModel.Spec);
                return;
            }

            SetSlotNotOccupied(occupiedSlotIndex);
        }

        void SetSlotNotOccupied(int updatedSlotIndex) {
            var (listener0, listener1) = _slotsOccupantsOnDeathEventsListeners[updatedSlotIndex];
            World.EventSystem.TryDisposeListener(ref listener0);
            World.EventSystem.TryDisposeListener(ref listener1);
            _slotsOccupantsOnDeathEventsListeners[updatedSlotIndex] = default;
            
            _slotsOccupiedStatuses.Down((uint)updatedSlotIndex);

            _slotsOccupantsAggressionScores[updatedSlotIndex] = float.NegativeInfinity;

            var slotOccupant = _slotsOccupants[updatedSlotIndex];
            _occupantEnemyRefToSlotIndexMap.Remove(slotOccupant);

            _slotsOccupants[updatedSlotIndex] = WeakModelRef<EnemyBaseClass>.Empty;

            if (slotOccupant.TryGet(out var enemy)) {
                enemy.OwnedCombatSlotIndex = -1;
            }

            int slotsCount = SlotsCount;
            for (int i = 0; i < slotsCount; i++) {
                var (indexOfNearestOccupiedNeighbour, _) = _slotNearestOccupiedSlotIndexAndDistanceSqArr[i];
                if (indexOfNearestOccupiedNeighbour == updatedSlotIndex) {
                    RecalculateSlotNearestOccupiedNeighbour(i);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RecalculateSlotNearestOccupiedNeighbour(int slotToRecalculateIndex) {
            var recalculatedSlotPosition = _slotsOriginalLocalPositions[slotToRecalculateIndex];
            float minDistanceSq = float.MaxValue;
            int minDistanceNeighbourIndex = -1;
            foreach (int occupiedSlotIndex in _slotsOccupiedStatuses.EnumerateOnes()) {
                var slotPos = _slotsOriginalLocalPositions[occupiedSlotIndex];
                var distanceSqToRecalculatedSlot = math.distancesq(recalculatedSlotPosition, slotPos);
                if ((distanceSqToRecalculatedSlot < minDistanceSq) & (occupiedSlotIndex != slotToRecalculateIndex)) {
                    minDistanceSq = distanceSqToRecalculatedSlot;
                    minDistanceNeighbourIndex = occupiedSlotIndex;
                }
            }

            _slotNearestOccupiedSlotIndexAndDistanceSqArr[slotToRecalculateIndex] = (minDistanceNeighbourIndex, minDistanceSq);
        }

        void OnOccupantDeath(DamageOutcome damageOutcome) {
            if (damageOutcome.Target is not NpcElement npcElement) {
                Log.Important?.Error($"{nameof(IAlive)} is null or not of type {nameof(NpcElement)}. It should always be of type {nameof(NpcElement)}");
                return;
            }

            SetSlotNotOccupied(npcElement.EnemyBaseClass);
        }

        void OnOccupantDeath(Model model) {
            if (model is not EnemyBaseClass enemyBase) {
                Log.Important?.Error($"{nameof(Model)} is null or not of type {nameof(EnemyBaseClass)}. It should always be of type {nameof(EnemyBaseClass)}");
                return;
            }

            if (enemyBase.NpcElement == null) {
                Log.Important?.Error($"{nameof(enemyBase.NpcElement)} is null");
                return;
            }

            SetSlotNotOccupied(enemyBase);
        }

        bool TryGetClosestAvailableSlotIndex(EnemyBaseClass enemy, float2 desiredPositionInSlotsSpace, float range, out int closestValidSlotIndex) {
            closestValidSlotIndex = -1;
            
            // Get slots which are valid on navmesh, in matching combat line, in matching in-fov status, not occupied
            var potentiallyValidSlots = GetPotentiallyValidSlots(enemy.KeepsInSecondLine, enemy.IsInHeroFov, false, ARAlloc.Temp);

            float closestDistanceSqToDesiredPos = float.MaxValue;
            var dynamicBlockingRange = math.max(BlockingRange, range);
            var dynamicBlockingRangeSq = math.square(dynamicBlockingRange);

            foreach (int slotIndex in potentiallyValidSlots.EnumerateOnes()) {
                var slotPosLocal = _slotsOriginalLocalPositions[slotIndex];
                var distanceSqToDesiredPos = math.distancesq(slotPosLocal, desiredPositionInSlotsSpace);
                if ((distanceSqToDesiredPos > closestDistanceSqToDesiredPos)) {
                    continue;
                }

                // Check for blockers. If found slot's nearest occupied neighbouring slot is closer than blocking range - found slot is blocked.   
                var (indexOfNearestOccupiedNeighbour, distanceSqToNearestOccupiedNeighbour) = _slotNearestOccupiedSlotIndexAndDistanceSqArr[slotIndex];
                if (((indexOfNearestOccupiedNeighbour != -1) & (distanceSqToNearestOccupiedNeighbour <= dynamicBlockingRangeSq))) {
                    continue;
                }

                closestValidSlotIndex = slotIndex;
                closestDistanceSqToDesiredPos = distanceSqToDesiredPos;
            }

            potentiallyValidSlots.Dispose();
            return closestValidSlotIndex != -1;
        }

        int GetBetterSlotIndex(EnemyBaseClass enemy, int currentSlotIndex, float2 desiredPositionInSlotsSpace, float range,
            float minimalNeededDistanceToDesiredPosChange) {
            
            var closestValidSlotIndex = currentSlotIndex;
            var currentSlotPosLocal = currentSlotIndex != -1 ? _slotsOriginalLocalPositions[currentSlotIndex] : default;
            float currentSlotDistanceToDesiredPos = currentSlotIndex != -1 ? math.distance(desiredPositionInSlotsSpace, currentSlotPosLocal) : float.PositiveInfinity;
            float closestDistanceToDesiredPos = currentSlotDistanceToDesiredPos - minimalNeededDistanceToDesiredPosChange;
            // If current slot distance to desired position is already very close - do not try to find better slot
            if (closestDistanceToDesiredPos < minimalNeededDistanceToDesiredPosChange) {
                return closestValidSlotIndex;
            }
            var closestDistanceSqToDesiredPos = math.square(closestDistanceToDesiredPos);
            
            // Get slots which are valid on navmesh, in matching combat line, in matching in-fov status
            var potentiallyValidSlots = GetPotentiallyValidSlots(enemy.KeepsInSecondLine, enemy.IsInHeroFov, true, ARAlloc.Temp);

            var dynamicBlockingRangeSq = math.square(math.max(range, BlockingRange));

            var currentEnemyAggressionScore = enemy.AggressionScore;
            var currentEnemyAggressionScoreWithThreshold = currentEnemyAggressionScore * AggressionScoreThreshold;

            foreach (int slotIndex in potentiallyValidSlots.EnumerateOnes()) {
                var slotOccupantAggressionScore = _slotsOccupantsAggressionScores[slotIndex];
                if (slotOccupantAggressionScore > currentEnemyAggressionScore) {
                    continue;
                }

                var slotPosLocal = _slotsOriginalLocalPositions[slotIndex];
                var distanceSqToDesiredPos = math.distancesq(slotPosLocal, desiredPositionInSlotsSpace);
                if ((distanceSqToDesiredPos > closestDistanceSqToDesiredPos)) {
                    continue;
                }
                var distanceToCurrentSlot = math.distance(slotPosLocal, currentSlotPosLocal);
                // If enemy has current slot, check if found slot is not too far and too close from current slot
                if ((currentSlotIndex != -1) & ((distanceToCurrentSlot < range * RangeMinDistanceMultiplier) | (distanceToCurrentSlot > range * RangeMaxDistanceMultiplier))) {
                    continue;
                }

                var (nearestOccupiedNeighbourIndex, nearestOccupiedNeighbourDistanceSq) = _slotNearestOccupiedSlotIndexAndDistanceSqArr[slotIndex];

                // Check if slot is blocked.
                // Fast check: if found slot's closest occupied neighbour has higher aggression score compared to aggression score with threshold - found slot is blocked  
                if (((nearestOccupiedNeighbourIndex != currentSlotIndex) & (nearestOccupiedNeighbourIndex != -1) & (nearestOccupiedNeighbourDistanceSq <= dynamicBlockingRangeSq)) &&
                    _slotsOccupantsAggressionScores[nearestOccupiedNeighbourIndex] > currentEnemyAggressionScoreWithThreshold) {
                    continue;
                }

                // If found slot's closest occupied neighbour occupant does not have higher aggression score, then some slightly farther neighbour
                // slot can have occupant with higher aggression. So it is needed to perform expensive check. 
                if (HasNeighbourSlotWithHigherAggressionOccupant(slotIndex, currentEnemyAggressionScoreWithThreshold, range)) {
                    continue;
                }

                closestValidSlotIndex = slotIndex;
                closestDistanceSqToDesiredPos = distanceSqToDesiredPos;
            }

            return closestValidSlotIndex;
        }

        /// <summary>
        /// Returns Bitmask for combat slots with are valid on navmesh, in matching combat line, in matching in-fov status, not occupied (if <see cref="canClaimSlotIfMoreAggressive"/> is false)
        /// </summary>
        UnsafeBitmask GetPotentiallyValidSlots(bool enemyKeepsInSecondLine, bool isNpcInFov, bool canClaimSlotIfMoreAggressive, Allocator allocator) {
            var potentiallyValidSlots = canClaimSlotIfMoreAggressive ?
                _slotsValidOnNavmeshStatuses.DeepClone(allocator) :
                UnsafeBitmask.AndWithInvertRightBitmask(_slotsValidOnNavmeshStatuses, _slotsOccupiedStatuses, allocator);
            // If enemy keeps second line - do not filter by fov.
            if (enemyKeepsInSecondLine == false) {
                if (isNpcInFov) {
                    UnsafeBitmask.And(ref potentiallyValidSlots, _slotsInFovStatuses);
                } else {
                    UnsafeBitmask.AndWithInvertRightBitmask(ref potentiallyValidSlots, _slotsInFovStatuses);
                }
            }

            // Filter slots by line. If enemy keeps second line, exclude all slots which are from the first line and vice versa. 
            var slotsToExcludeRangeStartIndex = math.select((uint)_vHeroCombatSlots.SecondLineSlotsStartIndex, 0, enemyKeepsInSecondLine);
            var slotsToExcludeRangeEnd = math.select((uint)SlotsCount, (uint)_vHeroCombatSlots.SecondLineSlotsStartIndex, enemyKeepsInSecondLine);
            var slotsToExcludeRangeLength = slotsToExcludeRangeEnd - slotsToExcludeRangeStartIndex;
            potentiallyValidSlots.Down(slotsToExcludeRangeStartIndex, slotsToExcludeRangeLength);
            return potentiallyValidSlots;
        }

        void OccupySlotAndReleaseNeighbourSlots(EnemyBaseClass enemy, int slotToOccupyIndex, float range) {
            if (_slotsOccupiedStatuses[(uint)slotToOccupyIndex]) {
                // Release slot at closestValidSlotIndex
                SetSlotNotOccupied(slotToOccupyIndex);
            }

            // Release all slots in blocking range from closestValidSlotIndex
            SetAllNeighbourSlotsNotOccupied(slotToOccupyIndex, range);

            // Occupy slot at closestValidSlotIndex
            SetSlotOccupied(slotToOccupyIndex, enemy);
        }

        void SetAllNeighbourSlotsNotOccupied(int mainSlotIndex, float range) {
            var rangeSq = math.square(range);
            var mainSlotPosLocal = _slotsOriginalLocalPositions[mainSlotIndex];
            foreach (int occupiedSlotIndex in _slotsOccupiedStatuses.EnumerateOnes()) {
                var slotPosLocal = _slotsOriginalLocalPositions[occupiedSlotIndex];
                var distanceSqToCheckedSlot = math.distancesq(slotPosLocal, mainSlotPosLocal);
                if ((distanceSqToCheckedSlot > rangeSq) | (occupiedSlotIndex == mainSlotIndex)) {
                    continue;
                }

                SetSlotNotOccupied(occupiedSlotIndex);
            }
        }

        bool HasNeighbourSlotWithHigherAggressionOccupant(int checkedSlotIndex, float checkedSlotOccupantAggressionScore, float range) {
            var rangeSq = math.square(range);
            var checkedSlotPosLocal = _slotsOriginalLocalPositions[checkedSlotIndex];
            foreach (int occupiedSlotIndex in _slotsOccupiedStatuses.EnumerateOnes()) {
                var slotPosLocal = _slotsOriginalLocalPositions[occupiedSlotIndex];
                var distanceSqToCheckedSlot = math.distancesq(slotPosLocal, checkedSlotPosLocal);
                if ((distanceSqToCheckedSlot > rangeSq) | (occupiedSlotIndex == checkedSlotIndex)) {
                    continue;
                }

                var slotOccupantAggressionScore = _slotsOccupantsAggressionScores[occupiedSlotIndex];
                if (slotOccupantAggressionScore > checkedSlotOccupantAggressionScore) {
                    return true;
                }
            }

            return false;
        }

        static float2 TransformWordPosToSlotsPos(float3 posInWorld, float3 heroCoords) {
            return posInWorld.xz - heroCoords.xz;
        }

        static void GetSlotNewPositionOnNavmesh(GraphNode heroPositionNode, Vector3 originalLocalPos, Vector3 heroPosition, Vector3 heroVelocity, float maxPositionDiff,
            out Vector3 desiredPosition, out bool isPositionValidOnNavmesh, out bool isHeroReachableFromNewPosition) {
            Vector3 initialDesiredPosition = heroPosition + originalLocalPos + heroVelocity;
            desiredPosition = initialDesiredPosition;
            var directionToParentNormalized = -originalLocalPos;
            float originalMaxNearestNodeDistance = AstarPath.active.maxNearestNodeDistance;
            AstarPath.active.maxNearestNodeDistance = VHeroCombatSlots.CombatSlotOffset;
            
            float distancePercentage = 0;
            NNInfo resultNode;
            do {
                desiredPosition += directionToParentNormalized * distancePercentage;
                resultNode = AstarPath.active.GetNearest(desiredPosition, Constraint);
                distancePercentage += PositionUpdateInterval;
            } while (!IsPathPossible(heroPositionNode, resultNode.node) && distancePercentage <= MaxPositionUpdates);
            
            if(distancePercentage > MaxPositionUpdates) {
                distancePercentage = 0;
                desiredPosition = initialDesiredPosition;
                AstarPath.active.maxNearestNodeDistance = 1;
                do {
                    distancePercentage += PositionUpdateInterval;
                    desiredPosition += Vector3.up * MaxVerticalDistance * distancePercentage;
                    resultNode = AstarPath.active.GetNearest(desiredPosition, Constraint);
                } while (resultNode.node == null && distancePercentage <= MaxVerticalPositionUpdates);
            }
            
            AstarPath.active.maxNearestNodeDistance = originalMaxNearestNodeDistance;
            
            isHeroReachableFromNewPosition = IsPathPossible(heroPositionNode, resultNode.node);
            desiredPosition = resultNode.node != null ? resultNode.position : initialDesiredPosition;
            isPositionValidOnNavmesh = resultNode.node != null && Vector2.Distance(desiredPosition.xz(), initialDesiredPosition.xz()) < maxPositionDiff;;
        }
        
        static bool IsPathPossible(GraphNode node1, GraphNode node2) {
            if (node1 == null || node2 == null) {
                return false;
            }
            return PathUtilities.IsPathPossible(node1, node2);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_slotsOriginalLocalPositions.IsCreated) {
                _slotsOriginalLocalPositions.Dispose();
            }

            if (_slotsPositions.IsCreated) {
                _slotsPositions.Dispose();
            }

            if (_slotNearestOccupiedSlotIndexAndDistanceSqArr.IsCreated) {
                _slotNearestOccupiedSlotIndexAndDistanceSqArr.Dispose();
            }

            if (_slotsOccupantsAggressionScores.IsCreated) {
                _slotsOccupantsAggressionScores.Dispose();
            }

            if (_slotsOccupantsOnDeathEventsListeners != null) {
                for (int i = 0; i < _slotsOccupantsOnDeathEventsListeners.Length; i++) {
                    var (listener0, listener1) = _slotsOccupantsOnDeathEventsListeners[i];
                    World.EventSystem.TryDisposeListener(ref listener0);
                    World.EventSystem.TryDisposeListener(ref listener1);
                    _slotsOccupantsOnDeathEventsListeners[i] = default;
                }
            }
            
            _slotsInFovStatuses.DisposeIfCreated();
            _slotsOccupiedStatuses.DisposeIfCreated();
            _slotsValidOnNavmeshStatuses.DisposeIfCreated();
            _slotsHeroReachableStatuses.DisposeIfCreated();
        }

#if UNITY_EDITOR
        public void DrawPlaymodeGizmos() {
            int slotsCount = SlotsCount;
            for (uint i = 0; i < slotsCount; i++) {
                var slotWorldPos = _slotsPositions[(int)i];
                if (_slotsValidOnNavmeshStatuses[i] == false) {
                    Gizmos.color = Color.black;
                } else if (_slotsOccupiedStatuses[i]) {
                    Gizmos.color = Color.red;
                } else if (_slotNearestOccupiedSlotIndexAndDistanceSqArr[(int)i].distanceSq < BlockingRangeSq) {
                    Gizmos.color = new Color(1, 0.5f, 0);
                } else if (_slotsInFovStatuses[i]) {
                    Gizmos.color = Color.green;
                } else {
                    Gizmos.color = Color.gray;
                }

                Gizmos.DrawSphere(slotWorldPos, GizmosSphereSize);
            }

            Gizmos.color = Color.white;
        }
#endif
    }
}