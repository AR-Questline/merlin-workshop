using Awaken.TG.Main.Animations.FSM.Npc.Base;

namespace Awaken.TG.Main.AI.States.ReturnToSpawn {
    public class StateReturnTauntFromAlert : StateTaunt {
        protected override NpcStateType StateToEnter => NpcStateType.Taunt;
    }
}