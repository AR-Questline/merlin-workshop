using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class GuardIntervention : EnemyBehaviourBase, IBehaviourBase {
        const float DistanceToTriggerStory = 5f;

        public static bool InterventionInProgress {
            get {
                foreach (var intervention in CurrentInterventions) {
                    if (intervention._inStory) {
                        return true;
                    }
                }
                return false;
            }
        }
        static readonly List<GuardIntervention> CurrentInterventions = new(16);
        static bool s_bulkStoppingIntervention;
        
        [SerializeField] StoryBookmark guardStory;

        NoMoveAndRotateTowardsTarget _noMoveAndRotate;
        KeepPosition _keepPosition;
        bool _isGuard;
        bool _inStory;

        public StoryBookmark GuardStory => guardStory;
        public override int Weight => 9999;
        public override int Priority => CombatBehaviourPriority.MostImportant;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => true;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => true;

        public bool CanIntervene => _isGuard && Hero.Current.HeroCombat.CanGuardIntervene(Npc) && ShouldIntervene;

        bool ShouldIntervene => Npc.ParentModel.DefaultOwner is {} owner && !CrimeUtils.HasCommittedUnforgivableCrime(owner) && CrimeUtils.HasBounty(owner);
        bool InCorrectPosition => _targetPosition.Contains(Npc.Coords);
        CharacterPlace _targetPosition;
        IEventListener _heroInvolvementListener;

        public static void EDITOR_RuntimeReset() {
            s_bulkStoppingIntervention = false;
        }

        protected override void OnInitialize() {
            _noMoveAndRotate = new NoMoveAndRotateTowardsTarget();
            _keepPosition = new KeepPosition(default, VelocityScheme.Walk, KeepPosition.DefaultMaxStrafeDistance, VHeroCombatSlots.CombatSlotOffset, VelocityScheme.Trot);
            _isGuard = CrimeReactionUtils.IsGuard(ParentModel.NpcElement);
        }

        protected override bool StartBehaviour() {
            if (s_bulkStoppingIntervention) {
                return false;
            }
            CurrentInterventions.Add(this);
            ParentModel.NpcElement.AddElement(new DisableAggroMusicMarker());
            if (!TryTriggerStory()) {
                _heroInvolvementListener = World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<IHeroInvolvement>(), this, OnHeroInvolvementAdded);
                UpdateTargetPosition();
                _keepPosition.UpdatePlace(_targetPosition);
                ParentModel.NpcMovement.ChangeMainState(_keepPosition);
                ParentModel.SetAnimatorState(NpcStateType.Movement);
            }
            return true;
        }
        public override void Update(float deltaTime) {
            if (_inStory) {
                if (ParentModel.NpcMovement.CurrentState != _noMoveAndRotate) {
                    ParentModel.NpcMovement.ChangeMainState(_noMoveAndRotate);
                }
                return;
            }
            
            if (!CanIntervene) {
                StopIntervention(ShouldIntervene);
                return;
            }
            
            if (TryTriggerStory()) {
                return;
            }
            
            if (ParentModel.NpcMovement.CurrentState != _keepPosition) {
                ParentModel.NpcMovement.ChangeMainState(_keepPosition);
            }

            UpdateTargetPosition();
            if (!InCorrectPosition) {
                _keepPosition.UpdatePlace(_targetPosition);
            }
        }

        public override void StopBehaviour() {
            if (_heroInvolvementListener != null) {
                World.EventSystem.DisposeListener(ref _heroInvolvementListener);
            }
            if (!s_bulkStoppingIntervention) {
                CurrentInterventions.Remove(this);
            }
            ParentModel.NpcElement.RemoveElementsOfType<DisableAggroMusicMarker>();
        }

        public static void StopInterventions(bool shouldIntervene) {
            if (s_bulkStoppingIntervention) {
                return;
            }
            s_bulkStoppingIntervention = true;
            foreach (var intervention in CurrentInterventions) {
                intervention.StopIntervention(shouldIntervene);
            }
            CurrentInterventions.Clear();
            s_bulkStoppingIntervention = false;
            
            if (!shouldIntervene) {
                AIUtils.ForceStopCombatWithHero();
            }
        }

        public override bool UseConditionsEnsured() => ParentModel.NpcElement?.IsTargetingHero() == true && CanIntervene;
        
        bool TryTriggerStory() {
            if (ParentModel.DistanceToTarget > DistanceToTriggerStory) return false;
            
            if (World.HasAny<IHeroInvolvement>()) {
                return false;
            }
            
            ParentModel.NpcMovement.ResetMainState(_keepPosition);
            AIUtils.RemoveAllNegativeStatusesFromCombatWithHero();

            HeroCombatSlots heroCombatSlots = Hero.Current.CombatSlots;
            foreach (var guardIntervention in CurrentInterventions) {
                if (guardIntervention == this) {
                    continue;
                }
                heroCombatSlots.ReleaseCombatSlot(guardIntervention.ParentModel);
            }
            if (ParentModel.TrySetBetterCombatSlot(2f, out var slotPosition)) {
                ParentModel.SetDesiredPosition(slotPosition);
            }
                
            var story = Story.StartStory(StoryConfig.Location(ParentModel.ParentModel, guardStory, typeof(VDialogue)));
            if (story.HasBeenDiscarded) {
                OnEndStory();
            } else {
                _inStory = true;
                story.ListenTo(Model.Events.AfterDiscarded, OnEndStory, this);
                this.ListenTo(Model.Events.AfterDiscarded, _ => story.FinishStory(), story);
            }
            Hero.Current.Trigger(CrimePenalties.Events.CrimePenaltyGuardCaught, ParentModel.NpcElement.ParentModel.DefaultOwner); 
            return true;
        }

        void OnEndStory() {
            var shouldIntervene = ShouldIntervene;
            if (shouldIntervene) {
                Hero.Current.HeroCombat.NotifyGuardIntervention(ParentModel.NpcElement);
            } else {
                Hero.Current.HeroCombat.NotifyBountyPaid(ParentModel.NpcElement);
            }
            _inStory = false;
            StopInterventions(shouldIntervene);
        }

        void StopIntervention(bool aggressively) {
            if (ParentModel == null) {
                return;
            }

            if (aggressively) {
                ParentModel.StopCurrentBehaviour(true);
            } else {
                ParentModel.StopCurrentBehaviour(false);
                ParentModel.NpcAI.ExitCombat(true, true);
            }
        }

        void OnHeroInvolvementAdded(Model model) {
            if (ParentModel.TrySetBetterCombatSlot(1.25f, out var slotPosition)) {
                ParentModel.SetDesiredPosition(slotPosition);
            }
            _targetPosition = KeepPositionBehaviour.GetTargetPosition(ParentModel, DistanceToTriggerStory * 0.5f);
            _keepPosition.UpdatePlace(_targetPosition);
        }
        
        void UpdateTargetPosition() {
            if (World.HasAny<IHeroInvolvement>()) {
                _targetPosition = KeepPositionBehaviour.GetTargetPosition(ParentModel, DistanceToTriggerStory * 0.5f);
                return;
            }
            _targetPosition = new CharacterPlace(ParentModel.NpcElement.GetCurrentTarget().Coords, DistanceToTriggerStory * 0.5f);
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<GuardIntervention> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.None.Yield();

            // === Constructor
            public Editor_Accessor(GuardIntervention behaviour) : base(behaviour) { }
        }
    }
}