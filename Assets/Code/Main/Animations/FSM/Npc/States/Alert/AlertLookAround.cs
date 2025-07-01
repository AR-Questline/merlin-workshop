using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Alert {
    public partial class AlertLookAround : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AlertLookAround;

        const float HeroVisibleTimeToLookAt = 1.25f;

        float _heroVisibleTime;
        
        public override NpcStateType Type => NpcStateType.AlertLookAround;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _heroVisibleTime = 0;
            Npc.NpcAI.ObserveAlertTarget = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (AlertLookAt.CanChangeToLookAt(Npc)) {
                _heroVisibleTime += deltaTime;
                if (_heroVisibleTime >= HeroVisibleTimeToLookAt) {
                    ParentModel.SetCurrentState(NpcStateType.AlertLookAt);
                }
            } else if (_heroVisibleTime > 0) {
                _heroVisibleTime -= deltaTime;
            }
        }
    }
}