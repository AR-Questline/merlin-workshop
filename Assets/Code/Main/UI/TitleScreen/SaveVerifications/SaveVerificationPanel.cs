using System;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.UI.TitleScreen.SaveVerifications {
    [SpawnsView(typeof(VSaveVerificationPanel))]
    public class SaveVerificationPanel : Element<TitleScreenUI> {
        readonly UniTask _task;
        readonly Progress<float> _progress;
        
        public Progress<float> Progress => _progress;
        
        public SaveVerificationPanel(UniTask task, Progress<float> progress) {
            _task = task;
            _progress = progress;
        }
        
        protected override void OnFullyInitialized() {
            WaitAndDiscard().Forget();
        }
        
        async UniTaskVoid WaitAndDiscard() {
            await _task;
            Discard();
        }
    }
}
