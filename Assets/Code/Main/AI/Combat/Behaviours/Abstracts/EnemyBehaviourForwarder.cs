using System;
using System.Collections.Generic;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    [Serializable]
    public abstract partial class EnemyBehaviourForwarder : EnemyBehaviourBase {
        [SerializeField] bool ignoreBaseBehaviourConditions;
        EnemyBehaviourBase _behaviourRef;
        
        protected abstract EnemyBehaviourBase BehaviourToClone { get; set; }
        protected abstract bool AdditionalConditions { get; } 
        protected override bool HideSerializableFields => true;
        
        // === Properties
        public override void CopyPropertiesTo(Model behaviourBase) {
            ((EnemyBehaviourForwarder)behaviourBase).BehaviourToClone = BehaviourToClone.Copy();
            base.CopyPropertiesTo(behaviourBase);
        }

        // === IBehaviourBase
        public override int Weight => _behaviourRef.Weight;
        public override int Priority => _behaviourRef.Priority;
        public override bool CanBeInterrupted => _behaviourRef.CanBeInterrupted;
        public override bool AllowStaminaRegen => _behaviourRef.AllowStaminaRegen;
        public override bool RequiresCombatSlot => _behaviourRef.RequiresCombatSlot;
        public override bool CanBeAggressive => _behaviourRef.CanBeAggressive;
        public override bool IsPeaceful => _behaviourRef.IsPeaceful;
        public override bool CanBlockDamage => _behaviourRef.CanBlockDamage;

        protected override void OnInitialize() { 
            _behaviourRef = ParentModel.AddElement(BehaviourToClone.Copy());
            _behaviourRef.AddElement<EnemyBehaviourForwardedMarker>();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_behaviourRef is { HasBeenDiscarded: false }) {
                _behaviourRef.Discard();
            }
        }

        public override void Update(float deltaTime) => _behaviourRef.Update(deltaTime);
        public override bool Start() => _behaviourRef.Start();
        public override bool UseConditionsEnsured() => ForwardedCanBeInvoked() && AdditionalConditions;
        protected override bool StartBehaviour() { throw new NotImplementedException(); }
        public override void StopBehaviour() => _behaviourRef.StopBehaviour();
        public override void BehaviourInterrupted() => _behaviourRef.BehaviourInterrupted();
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) => _behaviourRef.TriggerAnimationEvent(animationEvent);

        bool ForwardedCanBeInvoked() {
            return !_behaviourRef.DisabledForever &&
                   !_behaviourRef.HasElement<EnemyBehaviourCooldown>() &&
                   (ignoreBaseBehaviourConditions || _behaviourRef.UseConditionsEnsured());
        }

        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);
        
        public new class Editor_Accessor : Editor_Accessor<EnemyBehaviourForwarder> {
            EnemyBehaviourBase.Editor_Accessor _behaviourAccessor;
            EnemyBehaviourBase.Editor_Accessor BehaviourAccessor => _behaviourAccessor ??= Behaviour?.BehaviourToClone?.GetEditorAccessor();
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => BehaviourAccessor?.StatesUsedByThisBehaviour ?? new[] { NpcStateType.None };
            public override string GetName() => $"{nameof(EnemyBehaviourForwarder)} {BehaviourAccessor?.GetName()}";

            // === Constructor
            public Editor_Accessor(EnemyBehaviourForwarder behaviour) : base(behaviour) { }
        }
    }
}