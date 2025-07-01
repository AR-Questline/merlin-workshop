using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Block {
    public abstract partial class BlockStateBase : HeroAnimatorState {
        public override bool UsesActiveLayerMask => true;
    }
}