using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using System.Threading;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI {
    [SpawnsView(typeof(VBlurBackground))]
    public class BlurBackground : Model {
        public override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.Globals;
        
        VBlurBackground View => View<VBlurBackground>();
        public Transform CurrentTargetContent { get; private set; }
        public readonly BlurConfig config;
        public CancellationTokenSource BlurCancellationTokenSource { get; private set; }
        public static bool WithoutBlur => PlatformUtils.IsConsole || PlatformUtils.sDebugConsolePlatform || World.Any<BlurBackgroundSetting>()?.Enabled == false;

        public BlurBackground(Model model, BlurConfig config) {
            model.ListenTo(Events.BeforeDiscarded, HideBackground);
            this.config = config;
        }

        public void ShowBackground(View view) {
            ShowBackground(view.transform);
        }

        public void ShowBackground(Transform content) {
            CurrentTargetContent = content;
            
            BlurCancellationTokenSource?.Cancel();
            BlurCancellationTokenSource?.Dispose();
            BlurCancellationTokenSource = new CancellationTokenSource();
            
            if (config.delayFrames > 0 && !WithoutBlur) {
                View.CreateBackgroundWithDelay(config.delayFrames, BlurCancellationTokenSource.Token).Forget();
                return;
            } 
            
            View.CreateBackground();
        }
        
        public void HideBackground() {
            if (!HasBeenDiscarded) {
                Discard();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            BlurCancellationTokenSource?.Cancel();
            BlurCancellationTokenSource?.Dispose();
            BlurCancellationTokenSource = null;
        }
    }
    
    public struct BlurConfig {
        public int delayFrames;
        public bool useBlurVolume;
        public bool isOpaque;

        BlurConfig(int delayFrames, bool useBlurVolume, bool isOpaque) {
            this.delayFrames = delayFrames;
            this.useBlurVolume = useBlurVolume;
            this.isOpaque = isOpaque;
        }
        
        public static BlurConfig Default => new (1, false, true);
        public static BlurConfig WithBlurVolume => new (1, true, true);
        public static BlurConfig NonOpaque => new (1, false, false);
    }
}