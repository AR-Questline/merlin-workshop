using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.AnimalBehaviours {
    [Serializable]
    public partial class PreyAnimalRunAwayTemporary : PreyAnimalRunAway {
        const float StopWhenAnyOtherBehaviourAvailableInterval = 0.5f;
        
        [SerializeField] bool stopEveryNSeconds;
        [SerializeField, ShowIf(nameof(stopEveryNSeconds)), Range(0.1f, 999f)] float stopEveryNSecondsDuration = 1f;
        [SerializeField] bool stopWhenAnyOtherBehaviourAvailable = true;
        
        float _inStateDuration;
        float _anyOtherBehaviourAvailableCheckDelay;
        
        protected override bool StartBehaviour() {
            _inStateDuration = 0;
            _anyOtherBehaviourAvailableCheckDelay = StopWhenAnyOtherBehaviourAvailableInterval;
            return base.StartBehaviour();
        }
        
        public override void Update(float deltaTime) {
            if (stopEveryNSeconds) {
                _inStateDuration += deltaTime;
                if (_inStateDuration > stopEveryNSecondsDuration) {
                    _inStateDuration = 0;
                    if (ParentModel.TryToStartNewBehaviourExcept(this)) {
                        return;
                    }
                }
            }

            if (stopWhenAnyOtherBehaviourAvailable) {
                _anyOtherBehaviourAvailableCheckDelay -= deltaTime;
                if (_anyOtherBehaviourAvailableCheckDelay <= 0f) {
                    _anyOtherBehaviourAvailableCheckDelay = StopWhenAnyOtherBehaviourAvailableInterval;
                    if (ParentModel.TryToStartNewBehaviourExcept(this)) {
                        return;
                    }
                }
            }
            
            base.Update(deltaTime);
        }
    }
}
