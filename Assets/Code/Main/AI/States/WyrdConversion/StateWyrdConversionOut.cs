using Awaken.TG.Main.Animations.FSM.Npc.Base;

namespace Awaken.TG.Main.AI.States.WyrdConversion {
    public sealed class StateWyrdConversionOut : StateWyrdConversionBase {
        protected override bool IsConvertingIntoWyrd => false;
        protected override NpcStateType StateToEnter => NpcStateType.WyrdConversionOut;
    }
}