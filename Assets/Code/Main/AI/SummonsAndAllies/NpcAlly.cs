using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Pathfinding;
using UnityEngine;
using Patrol = Awaken.TG.Main.AI.Movement.States.Patrol;

namespace Awaken.TG.Main.AI.SummonsAndAllies {
    public partial class NpcAlly : Element<NpcElement>, ICanMoveProvider, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.NpcAlly;

        const float PatrolAcceptRange = VHeroCombatSlots.FirstLineCombatSlotOffset * 1.5f;
        const float TickSecondsInterval = 2.5f;
        const float MaxDistanceFromAlly = 15f;
        const float MaxDistanceFromAllySqr = MaxDistanceFromAlly * MaxDistanceFromAlly;
        const float MaxDistanceFromAllySqrDoubled = MaxDistanceFromAllySqr * 2 * 2;
        const float MaxDistanceFromAllySqrTripled = MaxDistanceFromAllySqr * 3 * 3;

        [Saved] WeakModelRef<ICharacter> _ally;
        Patrol _patrol;
        float _nextTickTime = float.MinValue;
        bool _factionOverriden;
        bool _isPlatformMoving;
        ITimeDependentDisabler _timeDependentDisablerParent;
        
        protected bool _movementPrevented;

        public virtual bool CanMove { get; protected set; }
        protected virtual bool AdditionalMovePrevent => false;
        [CanBeNull] public ICharacter Ally => _ally.Get();
        protected virtual bool AlwaysUpdate => false;
        protected float DistanceToAllySqr => Ally != null ? (Ally.Coords - ParentModel.Coords).sqrMagnitude : 0;

        public new static class Events {
            public static readonly Event<ICharacter, NpcAlly> BeforeAllyDeath = new(nameof(BeforeAllyDeath));
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        protected NpcAlly() { }

        public NpcAlly(ICharacter ally) {
            _ally = new WeakModelRef<ICharacter>(ally);
        }

        protected override void OnInitialize() {
            Init();
        }

        protected override void OnRestore() {
            Init();
        }

        protected virtual void Init() {
            NpcCanMoveHandler.AddCanMoveProvider(ParentModel, this);
            var location = ParentModel.ParentModel;
            location.OnVisualLoaded(AfterVisualLoaded, this);
            location.ListenTo(MovingPlatform.Events.MovingPlatformAdded, OnMovingPlatformAdded, this);
            location.ListenTo(MovingPlatform.Events.MovingPlatformDiscarded, OnMovingPlatformDiscarded, this);
        }

        protected virtual void AfterVisualLoaded(Transform parentTransform) {
            if (Ally == null) {
                Discard();
                return;
            }
            FloatRange waitDuration = new(3, 6);
            _patrol = new Patrol(new CharacterPlace(Ally.Coords, PatrolAcceptRange), MaxDistanceFromAlly / 2f, VelocityScheme.Walk, waitDuration);
            _patrol.UpdatePlace(_patrol.RandomDestination());
            ChangeFaction();
            FindTarget();
            Ally.ListenTo(AITargetingUtils.Relations.IsTargetedBy.Events.Changed, OnTargetingChanged, this);
            World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
            _timeDependentDisablerParent = ParentModel.ParentModel;
            ParentModel.ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, StayCloseToAlly, this);
            ParentModel.ListenTo(ICharacter.Events.CombatExited, OnCombatExit, this);
            ParentModel.ListenTo(IAlive.Events.BeforeDeath, OnBeforeDeath, this);
            IgnoreCollisionsWithHero();
        }

        void OnMovingPlatformAdded(MovingPlatform movingPlatform) {
            movingPlatform.ListenTo(MovingPlatform.Events.MovingPlatformStateChanged, OnPlatformStateChanged, this);
            OnPlatformStateChanged(movingPlatform.IsMoving);
        }
        
        void OnMovingPlatformDiscarded(MovingPlatform movingPlatform) {
            OnPlatformStateChanged(false);
        }

        void OnPlatformStateChanged(bool isMoving) {
            _isPlatformMoving = isMoving;

            if (!isMoving) {
                // defer running the rest of the code after elevator movement is done
                _nextTickTime = Time.unscaledTime;
            }
        }

        // === Lifecycle
        public void UnityUpdate() {
            if (!AlwaysUpdate && _timeDependentDisablerParent.TimeUpdatesDisabled) {
                return;
            }
            
            CanMove = !_movementPrevented && (ParentModel.IsInCombat() || !_isPlatformMoving) && !AdditionalMovePrevent;

            if (AstarPath.active == null) {
                return;
            }
            
            if (_isPlatformMoving) {
                ParentModel.Movement.Controller.RichAI.destination = ParentModel.Coords;
                return;
            }

            if (_nextTickTime > Time.unscaledTime) {
                return;
            }
            
            _nextTickTime = Time.unscaledTime + TickSecondsInterval;
            FindTarget();

            if (ParentModel.GetCurrentTarget() != null) {
                float distanceToAllySqr = DistanceToAllySqr;
                if (distanceToAllySqr > MaxDistanceFromAllySqrTripled && ParentModel.Movement != null) {
                    ParentModel.ForceEndCombat();
                    TeleportToAlly(distanceToAllySqr, TeleportContext.AllyTooFar, out _);
                }
                return;
            }
            
            if (ParentModel.Movement != null && ParentModel.Movement.CurrentState != _patrol) {
                _patrol.UpdatePlace(ParentModel.Coords);
                ParentModel.Movement.ChangeMainState(_patrol);
            }

            StayCloseToAlly();
        }

        void StayCloseToAlly() {
            float distanceToAllySqr = DistanceToAllySqr;
            if (distanceToAllySqr > MaxDistanceFromAllySqrTripled && ParentModel.Movement != null) {
                TeleportToAlly(distanceToAllySqr, TeleportContext.AllyTooFar, out _);
                return;
            }

            if (distanceToAllySqr > MaxDistanceFromAllySqr) { 
                _patrol.UpdatePlace(Ally?.Coords ?? ParentModel.Coords); 
                _patrol.UpdateVelocityScheme(distanceToAllySqr > MaxDistanceFromAllySqrDoubled ? VelocityScheme.Run : VelocityScheme.Trot);
            } else {
                _patrol.UpdateVelocityScheme(VelocityScheme.Walk);
            }
        }

        protected void TeleportToAlly(float distanceToAllySqr, TeleportContext teleportContext, out Vector3 teleportPosition) {
            ICharacter ally = Ally;
            if (ally == null || ally.HasBeenDiscarded || AstarPath.active == null) {
                teleportPosition = ParentModel?.Coords ?? Vector3.zero;
                return;
            }
            teleportPosition = ally.Coords +
                               ally.Forward() * RandomUtil.UniformFloat(3f, 9f) +
                               ally.Right() * RandomUtil.UniformFloat(-3f, 3f);
            var nnInfo = AstarPath.active.GetNearest(teleportPosition);
            if (nnInfo.node != null) { teleportPosition = nnInfo.position; }
            var path = ABPath.Construct(ally.Coords, teleportPosition, p => OnTeleportPathCalculated(p, distanceToAllySqr, teleportContext));
            AstarPath.StartPath(path, assumeInPlayMode: true);
        }

        void OnTeleportPathCalculated(Path path, float distanceToAllySqr, TeleportContext teleportContext) {
            if (path.error) {
                Log.Important?.Error($"Failed to teleport summon {this} to ally {Ally}. \nPath calculation failed: {path.errorLog}");
                return;
            }
            if (HasBeenDiscarded || Ally == null || Ally.HasBeenDiscarded) {
                return;
            }
            ParentModel.Movement.Controller.TeleportTo(new TeleportDestination { position = path.vectorPath[^1] }, TeleportContext.AllyTooFar);
            _patrol.UpdatePlace(Ally.Coords); 
            _patrol.UpdateVelocityScheme(distanceToAllySqr > MaxDistanceFromAllySqrDoubled ? VelocityScheme.Run : VelocityScheme.Trot);
        }

        // === Listener Callbacks
        protected virtual bool CanRecalculateTarget() {
            return true;
        }
        
        protected void FindTarget() {
            ParentModel.Movement.ResetMainState(_patrol);

            foreach (var allyAttacker in Ally.PossibleAttackers) {
                ParentModel.TryAddPossibleCombatTarget(allyAttacker);
            }

            if (!CanRecalculateTarget()) {
                return;
            }
            
            if (ParentModel.NpcAI.InCombat) {
                ParentModel.RecalculateTarget(false);
            } else {
                var target = ParentModel.RecalculateTarget(true);
                if (target != null) {
                    ParentModel.NpcAI.EnterCombatWith(target, true);
                }
            }
        }

        void OnTargetingChanged(RelationEventData data) {
            if (ParentModel.GetCurrentTarget() == null && data is { newState: true, to: NpcElement }) {
                FindTarget();
            }
        }

        void OnCombatExit() {
            ParentModel.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
            _patrol.UpdatePlace(Ally.Coords);
        }
        
        void OnBeforeDeath() {
            Ally.Trigger(Events.BeforeAllyDeath, this);
        }

        // === Helpers
        void ChangeFaction() {
            if (ParentModel.Faction != Ally.Faction) {
                ParentModel.OverrideFaction(Ally.GetFactionTemplateForSummon(), FactionOverrideContext.Ally);
                _factionOverriden = true;
            }
        }

        void ResetFaction() {
            if (_factionOverriden) {
                ParentModel.ResetFactionOverride(FactionOverrideContext.Ally);
                _factionOverriden = false;
            }
        }

        void IgnoreCollisionsWithHero() {
            if (Ally is Hero hero && ParentModel.Controller.TryGetComponent(out MeshCollider meshCollider)) {
                Physics.IgnoreCollision(meshCollider, hero.VHeroController.Controller);
            }
        }

        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            NpcCanMoveHandler.RemoveCanMoveProvider(ParentModel, this);
            World.Services.TryGet<UnityUpdateProvider>()?.UnregisterGeneric(this);
            ResetFaction();
        }
    }
}
