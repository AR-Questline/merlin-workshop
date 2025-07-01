using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.DeathUI {
    [UsesPrefab("UI/VDeathTeleportUI")]
    public class VDeathTeleportUI : View<DeathUI>{
        const float FadeTime = 1.5f;
        
        [SerializeField] CanvasGroup mainContent, blackVignette;
        [SerializeField] public EventReference jailSound;
        
        DirectTimeMultiplier _timeMultiplier;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            if (!jailSound.IsNull) {
                FMODManager.PlayOneShot(jailSound);
            }
            SlowDownTime();
            FadeIn().Forget();
        }
        
        void SlowDownTime() {
            _timeMultiplier = new DirectTimeMultiplier(1, ID);
            World.Only<GlobalTime>().AddTimeModifier(_timeMultiplier);
            DOTween.To(() => Time.timeScale, _timeMultiplier.Set, 0f, FadeTime * 2f).SetUpdate(true);
        }
        
        async UniTaskVoid FadeIn() {
            mainContent.alpha = 0;
            blackVignette.alpha = 0;
            
            await blackVignette.DOFade(1, FadeTime).SetUpdate(true);
            await mainContent.DOFade(1, FadeTime).SetUpdate(true);
            await AsyncUtil.DelayTime(Target, 2, true);
            Hero.Current.HealthElement.Revive();
            
            await World.Services.Get<TransitionService>().ToBlack(TransitionService.DefaultFadeIn);
            MapChange();
            Target.Discard();
        }

        protected virtual void MapChange() {
            Portal.MapChangeTo(Hero.Current, Target.SceneToTeleport, World.Services.Get<SceneService>().ActiveSceneRef, Target.IndexTag);
        }
        
        protected override IBackgroundTask OnDiscard() {
            _timeMultiplier.Remove();
            return base.OnDiscard();
        }
    }
}
