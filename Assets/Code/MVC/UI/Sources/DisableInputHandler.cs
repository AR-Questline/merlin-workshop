using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC.UI.Events;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.MVC.UI.Sources {
    public partial class DisableInputHandler : AlwaysPresentHandlers, IUIAware {
        readonly int _millisecondsDelay;
        readonly int _framesDelay;

        // We need to delay the discard so don't pass owner to base constructor.
        public DisableInputHandler(IModel owner, int millisecondsDelay, int framesDelay) :
            base(UIContext.All, new InputBlock(), null, int.MaxValue) {
            _millisecondsDelay = millisecondsDelay;
            _framesDelay = framesDelay;
            owner?.ListenTo(Events.BeforeDiscarded, DelayedDiscard, this);
        }

        public void DelayedDiscard(Model _) {
            DelayedDiscardImpl().Forget();
        }

        async UniTaskVoid DelayedDiscardImpl() {
            var shouldDiscard = await AsyncUtil.DelayFrameOrTime(this, _framesDelay, _millisecondsDelay, true);
            if (shouldDiscard) {
                Discard();
            }
        }

        public UIResult Handle(UIEvent evt) {
            return UIResult.Prevent;
        }

        class InputBlock : IUIAware {
            public UIResult Handle(UIEvent evt) {
                return UIResult.Prevent;
            }
        }
    }
}
