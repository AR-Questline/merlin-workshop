using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    public partial class HeroInvolvedInputListener : Element<Story>, IUIAware {
        public sealed override bool IsNotSaved => true;

        readonly CancellationTokenSource _source;
        
        public HeroInvolvedInputListener(CancellationTokenSource source) {
            _source = source;
        }

        protected override void OnInitialize() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, this, 100));
            DiscardWhenTokenCanceled().Forget();
        }

        async UniTask DiscardWhenTokenCanceled() {
            await AsyncUtil.UntilCancelled(_source.Token);
            if (!HasBeenDiscarded) {
                Discard();
            }
        }

        public UIResult Handle(UIEvent evt) {
            switch (evt) {
                case UIKeyDownAction action when action.Data.actionName == KeyBindings.Gameplay.SkipDialogue:
                    _source.Cancel();
                    return UIResult.Accept;
                case UIEMouseDown {IsLeft: true}:
                    _source.Cancel();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }
    }
}