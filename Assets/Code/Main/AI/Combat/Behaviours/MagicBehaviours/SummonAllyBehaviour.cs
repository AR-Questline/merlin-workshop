using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class SummonAllyBehaviour : EnemyBehaviourBase, IBehaviourBase {
        const float MaxDistanceFromSummoner = 5f;
        
        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(-1, 100)] 
        [SerializeField] int weight = 10;

        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        [SerializeField, Range(1, 20), HideIf(nameof(HideIfGroupSpawn))] float maxAlliesSpawned = 2;
        [SerializeField] bool killWhenOwnerDies = true;
        [SerializeField] bool canBeInterrupted = true;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference spawnVFX;
        
        [InfoBox("Cannot be unique npc", InfoMessageType.Error, nameof(NotRepetitiveNpc))]
        [SerializeField, TemplateType(typeof(LocationTemplate)), HideIf(nameof(HideIfGroupSpawn))]
        TemplateReference allyToSpawn;
        [SerializeField] NpcStateType animatorStateType = NpcStateType.MagicProjectile;
        [SerializeField] bool rotateTowardsTarget = true;

        // === Properties & Fields
        public override int Weight => weight;
        public override bool CanBeInterrupted => canBeInterrupted;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => false;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
        bool HideIfGroupSpawn => this is SummonGroupOfAlliesBehaviour;

        bool NotRepetitiveNpc => RepetitiveNpcUtils.InvalidLocation(allyToSpawn);
        
        List<WeakModelRef<Location>> _spawnedAllies;
        MovementState _overrideMovementState;
        bool _spawnRight;

        protected override void OnInitialize() {
            _spawnRight = true;
            _spawnedAllies = new List<WeakModelRef<Location>>();
            ParentModel.NpcElement.ListenTo(IAlive.Events.BeforeDeath, DiscardAllies, this);
            this.ListenTo(Model.Events.BeforeDiscarded, DiscardAllies, this);
        }

        protected override bool StartBehaviour() {
            ParentModel.SetAnimatorState(animatorStateType);
            _overrideMovementState = ParentModel.NpcMovement.ChangeMainState(rotateTowardsTarget ? new NoMoveAndRotateTowardsTarget() : new NoMove());
            return true;
        }

        public override void Update(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != animatorStateType) {
                ParentModel.StartWaitBehaviour();
            }
        }

        public override void StopBehaviour() {
            ParentModel.NpcMovement.ResetMainState(_overrideMovementState);
        }
        
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                OnAnimationSpawnTriggered();
            }
        }

        public override bool UseConditionsEnsured() => !IsMuted && _spawnedAllies.Count < maxAlliesSpawned;

        // === Helpers
        protected virtual void OnAnimationSpawnTriggered() {
            GetSpawnPosition(out Vector3 position, out Quaternion rotation);
            SpawnSummon(allyToSpawn.Get<LocationTemplate>(), position, rotation);
        }
        
        void GetSpawnPosition(out Vector3 spawnPosition, out Quaternion spawnRotation) {
            Transform parentViewTransform = ParentModel.NpcElement.CharacterView.transform;
            Vector3 rightVector = parentViewTransform.transform.right * 3.5f;
            spawnPosition = _spawnRight ? ParentModel.Coords + rightVector : ParentModel.Coords - rightVector;
            _spawnRight = !_spawnRight;
            spawnRotation = parentViewTransform.rotation;
            
            // --- Find closest position on navmesh
            float originalMaxNearestNodeDistance = AstarPath.active.maxNearestNodeDistance;
            AstarPath.active.maxNearestNodeDistance = MaxDistanceFromSummoner;
            var resultNode = AstarPath.active.GetNearest(spawnPosition, NNConstraint.Walkable);
            spawnPosition = resultNode.node != null ? resultNode.position : spawnPosition;
            AstarPath.active.maxNearestNodeDistance = originalMaxNearestNodeDistance;
        }
        
        protected Location SpawnSummon(LocationTemplate toSpawn, Vector3 spawnPosition, Quaternion spawnRotation) {
            // --- Spawn
            Location ally = toSpawn.SpawnLocation(spawnPosition, spawnRotation);
            InitializeAlly(ally);
            
            // --- VFX
            PrefabPool.InstantiateAndReturn(spawnVFX, spawnPosition, spawnRotation).Forget();
            // --- Audio
            ParentModel.NpcElement.PlayAudioClip(AliveAudioType.SpecialRelease.RetrieveFrom(ParentModel.NpcElement), true);

            return ally;
        }

        void InitializeAlly(Location ally) {
            WeakModelRef<Location> allyRef = ally;
            _spawnedAllies.Add(allyRef);

            ally.TryGetElement<NpcElement>()?.AddElement(new NpcAISummon(ParentModel.NpcElement, 0));
            
            ally.AfterFullyInitialized(() => {
                if (ParentModel.HasBeenDiscarded) {
                    ally.Kill();
                    return;
                }
                NpcElement npc = ally.Element<NpcElement>();
                RepetitiveNpcUtils.Check(npc);
                npc.OverrideFaction(ParentModel.NpcElement.GetFactionTemplateForSummon(), FactionOverrideContext.Summon);
                npc.ListenTo(Model.Events.BeforeDiscarded, _ => _spawnedAllies.Remove(allyRef), this);
                npc.OnCompletelyInitialized(static npc => npc.NpcAI.EnterCombatWith(npc.GetCurrentTarget()));
            });
        }

        void DiscardAllies() {
            if (!killWhenOwnerDies) {
                return;
            }
            
            foreach (var ally in _spawnedAllies.ToList()) {
                if (ally.TryGet(out Location location)) {
                    location.Kill();
                }
            }
        }
        
        // === Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;
        
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<SummonAllyBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => Behaviour.animatorStateType.Yield();

            // === Constructor
            public Editor_Accessor(SummonAllyBehaviour behaviour) : base(behaviour) { }
        }
    }
}