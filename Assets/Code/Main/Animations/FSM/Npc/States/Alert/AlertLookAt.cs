using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Alert {
    public partial class AlertLookAt : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AlertLookAt;

        const float MinimumHeroVisibilityValue = 0.4f;
        const float LookAtOrAroundHeroVisibilityThresholdValue = 0.1f;
        const float HeroLoseTimeToLookAround = 0.75f;

        float _heroLoseTime;

        public override NpcStateType Type => NpcStateType.AlertLookAt;
        public static bool CanChangeToLookAt(NpcElement npc) => npc.NpcAI.MaxHeroVisibilityGain > LookAtOrAroundHeroVisibilityThresholdValue;
        public static NpcStateType GetStartingAlertState(NpcElement npc) => StartAsLookAt(npc) ? NpcStateType.AlertLookAt : NpcStateType.AlertLookAround;
        static bool StartAsLookAt(NpcElement npc) => npc.NpcAI.MaxHeroVisibilityGain > MinimumHeroVisibilityValue;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _heroLoseTime = 0f;
            Npc.NpcAI.ObserveAlertTarget = true;
        }

        protected override void OnUpdate(float deltaTime) {
            if (!CanChangeToLookAt(Npc)) {
                _heroLoseTime += deltaTime;
                if (_heroLoseTime >= HeroLoseTimeToLookAround) {
                    ParentModel.SetCurrentState(NpcStateType.AlertLookAround);
                }
            } else if (_heroLoseTime > 0) {
                _heroLoseTime -= deltaTime;
            }
        }
    }
}