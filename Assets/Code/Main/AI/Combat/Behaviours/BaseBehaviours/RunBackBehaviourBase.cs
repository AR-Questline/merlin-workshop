using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public abstract partial class RunBackBehaviourBase<TEnemy> : MovementBehaviour<TEnemy>, IRunBackBehaviour where TEnemy : EnemyBaseClass {
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 1;
        
        public override int Weight => weight;
        protected abstract float DistanceToStop { get; }
        protected Wander _wander;
        protected float _duration;
        
        protected override bool StartBehaviour() {
            _wander = new Wander(TargetPlace(), VelocityScheme.Run, true);
            ParentModel.NpcMovement.ChangeMainState(_wander);
            ParentModel.SetAnimatorState(NpcStateType.Movement);
            return true;
        }
        
        public override void StopBehaviour() {
            ParentModel.NpcMovement.ResetMainState(_wander);
            _wander = null;
            _duration = 0;
        }

        public override bool UseConditionsEnsured() {
            var hero = Hero.Current;
            return ParentModel.NpcElement.IsTargetingHero() &&
                   hero.CombatSlots.GetOccupiedSlotsCount() >= 1 &&
                   ParentModel.DistanceToTarget <= 3.5f;
        }

        protected CharacterPlace TargetPlace() {
            Vector3 destination;
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            if (target != null) {
                Vector3 targetPos = target.Coords;
                Vector3 direction = ParentModel.Coords - targetPos;
                destination = targetPos + direction * 3f;
                destination += ParentModel.ParentModel.ViewParent.right * (RandomUtil.UniformFloat(0, 100) > 50 ? -1 : 1);
            } else {
                destination = ParentModel.ParentModel.ViewParent.forward * -3f;
            }
            return new CharacterPlace(destination, 0.5f);
        }
    }
}