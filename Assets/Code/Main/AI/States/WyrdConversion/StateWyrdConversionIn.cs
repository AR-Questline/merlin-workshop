using Awaken.TG.Main.Animations.FSM.Npc.Base;

namespace Awaken.TG.Main.AI.States.WyrdConversion {
    public sealed class StateWyrdConversionIn : StateWyrdConversionBase {
        protected override bool IsConvertingIntoWyrd => true;
        protected override NpcStateType StateToEnter => NpcStateType.WyrdConversionIn;
    }
}