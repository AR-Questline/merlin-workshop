using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Base {
    public abstract partial class BaseAttackState : BaseCombatState {
        protected override void AfterEnter(float previousStateNormalizedTime) {
            Npc.Trigger(NpcElement.Events.AnimatorEnteredAttackState, Npc);
            if (Npc.SalsaEmitter != null) {
                Npc.SalsaEmitter.TriggerEmotion(SalsaEmotion.Attack);
            }
        }

        protected override void OnExit(bool restarted) {
            Npc.Trigger(NpcElement.Events.AnimatorExitedAttackState, Npc);
            base.OnExit(restarted);
        }
    }
}