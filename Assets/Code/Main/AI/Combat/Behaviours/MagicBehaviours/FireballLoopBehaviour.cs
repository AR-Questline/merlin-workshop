using System;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class FireballLoopBehaviour : FireballBehaviour {
        [SerializeField] int loopsToPerform = 3;
        [SerializeField] NpcStateType loopAnimatorStateType = NpcStateType.MagicLoopHold;
        [SerializeField] NpcStateType exitAnimatorStateType = NpcStateType.MagicLoopEnd;

        int _loopsToPerform;
        
        protected override bool IsInValidState => base.IsInValidState || NpcGeneralFSM.CurrentAnimatorState.Type == loopAnimatorStateType || NpcGeneralFSM.CurrentAnimatorState.Type == exitAnimatorStateType; 
        
        protected override bool StartBehaviour() {
            _loopsToPerform = loopsToPerform;
            return base.StartBehaviour();
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart) {
                SpawnFireBallInHand().Forget();
            } else if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                SpawnNextProjectile();
            }
        }

        void SpawnNextProjectile() {
            _loopsToPerform--;
            CastSpell(false);
            if (_loopsToPerform <= 0) {
                EndLoop();
            }
        }

        void EndLoop() {
            ReturnInstantiatedPrefabs();
            ParentModel.SetAnimatorState(exitAnimatorStateType);
        }
    }
}