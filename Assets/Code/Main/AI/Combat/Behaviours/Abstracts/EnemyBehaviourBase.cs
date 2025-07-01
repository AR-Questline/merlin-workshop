using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    [Serializable]
    public abstract partial class EnemyBehaviourBase : Element<EnemyBaseClass>, ICloneAbleModel, IBehaviourBase, IIsGroundedProvider {
        protected const string BasePropertiesGroup = "Base Properties";

        public sealed override bool IsNotSaved => true;
        
        // === Fields
        [BoxGroup(BasePropertiesGroup), ShowIf("@false")] 
        [SerializeField] public int saveIndex = -1;
        [BoxGroup(BasePropertiesGroup), HideIf(nameof(HideSerializableFields))] 
        [SerializeField] bool forceGrounded;
        
        string _saveID;
        NpcGeneralFSM _npcGeneralFSM;
        NpcCustomActionsFSM _customActionsFSM;
        NpcOverridesFSM _npcOverridesFSM;
        NpcTopBodyFSM _npcTopBodyFSM;
        
        // === Properties
        public EnemyBehaviourBase Copy() => (EnemyBehaviourBase) this.Clone();
        public virtual void CopyPropertiesTo(Model behaviourBase) {}
        public virtual int SpecialAttackIndex => 0;
        protected IAIEntity AIEntity => ParentModel.NpcElement.AIEntity;
        protected NpcGeneralFSM NpcGeneralFSM => ParentModel.NpcElement.CachedElement(ref _npcGeneralFSM);
        protected NpcCustomActionsFSM CustomActionsFSM => ParentModel.NpcElement.CachedElement(ref _customActionsFSM);
        protected NpcOverridesFSM NpcOverridesFSM => ParentModel.NpcElement.CachedElement(ref _npcOverridesFSM);
        protected NpcTopBodyFSM NpcTopBodyFSM => ParentModel.NpcElement.CachedElement(ref _npcTopBodyFSM);
        protected bool IsMuted => ParentModel.NpcElement.IsMuted;
        protected NpcElement Npc => ParentModel.NpcElement;
        public virtual bool DisabledForever {
            get => saveIndex != -1 && World.Services.Get<GameplayMemory>().Context().Get(SaveID, false);
            set => World.Services.Get<GameplayMemory>().Context().Set(SaveID, value);
        }
        protected virtual bool HideSerializableFields => false;

        string SaveID {
            get {
                if (string.IsNullOrWhiteSpace(_saveID)) {
                    _saveID = ParentModel.ID + $"_{saveIndex}";
                }
                return _saveID;
            }
        }

        // === IBehaviourBase
        public abstract int Weight { get; }
        public virtual int Priority => CombatBehaviourPriority.Default;
        public virtual bool CanMove => true;
        public bool CanBeInvoked => !DisabledForever && !HasElement<EnemyBehaviourCooldown>() && !HasElement<EnemyBehaviourForwardedMarker>() && UseConditionsEnsured();
        protected virtual bool ExposeWeakspot => false;
        public abstract bool CanBeInterrupted { get; }
        public abstract bool AllowStaminaRegen { get; }
        public abstract bool RequiresCombatSlot { get; }
        public abstract bool CanBeAggressive { get; }
        public abstract bool IsPeaceful { get; }
        public virtual bool CanBlockDamage => false;
        protected virtual CombatBehaviourCooldown Cooldown => CombatBehaviourCooldown.None;
        protected virtual float CooldownDuration => 1f;
        
        // === IIsGroundedProvider
        public bool IsGrounded => forceGrounded;
        
        public new static class Events {
            public static readonly Event<EnemyBehaviourBase, EnemyBehaviourBase> BehaviourExited = new(nameof(BehaviourExited));
        }

        public virtual bool Start() {
            ApplyCooldown();
            if (ExposeWeakspot) {
                ParentModel.Trigger(EnemyBaseClass.Events.ToggleWeakSpot, true);
            }
            if (forceGrounded) {
                NpcIsGroundedHandler.AddIsGroundedProvider(Npc, this);
            }
            return StartBehaviour();
        }
        
        public void Stop() {
            StopBehaviour();
            BehaviourExitInternal();
            this.Trigger(Events.BehaviourExited, this);
        }

        public void Interrupt() {
            BehaviourInterrupted();
            BehaviourExitInternal();
            this.Trigger(Events.BehaviourExited, this);
        }
        
        public virtual void Update(float deltaTime) { }
        public virtual void StopBehaviour() { }
        public virtual void BehaviourInterrupted() { }
        public virtual void TriggerAnimationEvent(ARAnimationEvent animationEvent) { }
        public abstract bool UseConditionsEnsured();
        protected abstract bool StartBehaviour();
        protected virtual void BehaviourExit() { }

        void BehaviourExitInternal() {
            if (ExposeWeakspot) {
                ParentModel.Trigger(EnemyBaseClass.Events.ToggleWeakSpot, false);
            }
            if (forceGrounded) {
                NpcIsGroundedHandler.RemoveIsGroundedProvider(Npc, this);
            }
            BehaviourExit();
        }
        
        void ApplyCooldown() {
            if (Cooldown == CombatBehaviourCooldown.UntilTimeElapsed) {
                EnemyBehaviourCooldown.Cooldown(this, new TimeDuration(CooldownDuration));
                return;
            }

            if (Cooldown == CombatBehaviourCooldown.UntilEndOfFight) {
                EnemyBehaviourCooldown.Cooldown(this, new UntilEndOfFightDuration(ParentModel.NpcElement));
                return;
            }

            if (Cooldown == CombatBehaviourCooldown.Forever) {
                DisabledForever = true;
            }
        }
        
        // === Editor
        public string Editor_GetName() => this.GetEditorAccessor().GetName();
        public abstract Editor_Accessor GetEditorAccessor();

        public abstract class Editor_Accessor {
            public abstract IEnumerable<NpcStateType> StatesUsedByThisBehaviour { get; }
            public abstract string GetName();
        }
        
        public abstract class Editor_Accessor<T> : Editor_Accessor where T : EnemyBehaviourBase {
            public override string GetName() {
                string name = Behaviour.GetType().Name.Replace("Behaviour", "");

                var state = StatesUsedByThisBehaviour.FirstOrDefault();
                if (state != NpcStateType.None) {
                    name += $" ({state})";
                }
                
                return name;
            }
            protected T Behaviour { get; }
            
            protected Editor_Accessor(T behaviour) {
                Behaviour = behaviour;
            }
        }
    }
}