using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    public abstract partial class BaseBossCombat : EnemyBaseClass, IRefreshedByAttachment<BossCombatAttachment>, ICloneAbleModel {
        bool _canLoseTargetBasedOnVisibility;
        
        // === Behaviours
        public int CurrentPhase { get; set; }
        public override bool CanLoseTargetBasedOnVisibility => _canLoseTargetBasedOnVisibility;
        protected override bool CanBePushed => false;
        protected bool InPhaseTransition { get; private set; }
        
        // === Copying
        public BaseBossCombat Copy() => (BaseBossCombat) this.Clone();
        public virtual void CopyPropertiesTo(Model behaviourBase) {
            BaseBossCombat baseBossCombat = (BaseBossCombat)behaviourBase;
            baseBossCombat.CombatBehaviours = new List<EnemyBehaviourBase>();
            baseBossCombat.CombatBehavioursReferences = new List<ARAssetReference>();
        }
        
        // === Initialization
        public virtual void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            BaseBossCombat baseClass = spec.BossBaseClass;
            _canLoseTargetBasedOnVisibility = spec.canLoseTargetBasedOnVisibility;
            WeaponsAlwaysEquippedBase = spec.weaponsAlwaysEquipped;
            canBeSlidInto = baseClass.canBeSlidInto;
        }
        
        protected override void AfterVisualLoaded(Transform parentTransform) {
            ChangeCombatData();
        }

        public override void OnWyrdConversionStarted() { }
        protected override void AddPersistentBehaviours() { }

        protected override UniTaskVoid ChangeCombatData(bool force = false) {
            TryChangeCombatData(NpcElement.FightingStyle.RetrieveCombatData(this)).Forget();
            return default;
        }
        
        // === LifeCycle
        protected void IncrementPhase() {
            SetPhase(CurrentPhase + 1);
        }

        protected void SetPhase(int phase) {
            StopCurrentBehaviour(false);
            CurrentPhase = phase;
            TryChangeCombatData(NpcElement.FightingStyle.RetrieveCombatData(this)).Forget();
            OnPhaseTransitionFinished(phase);
            InPhaseTransition = false;
        }
        
        protected void SetPhaseWithTransition(int phase, bool alternate = false) {
            InPhaseTransition = true;
            StopCurrentBehaviour(false);
            NpcElement.SetAnimatorState(NpcFSMType.GeneralFSM, alternate ? NpcStateType.PhaseTransitionAlternate : NpcStateType.PhaseTransition);
            NpcElement.ListenToLimited(PhaseTransition.Events.TransitionFinished, () => SetPhase(phase), this);
        }
        
        protected virtual void OnPhaseTransitionFinished(int phase) { }
        
        protected override void Tick(float deltaTime, NpcElement npc) {
            if (npc.GetCurrentTarget() == null) {
                StopCurrentBehaviour(false);
                return;
            }

            if (CurrentBehaviour.Get() == null) {
                SelectNewBehaviour();
            }
        }
    }
}