using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public abstract class VCCGridSelectOption<TTarget> : View<TTarget>, IUIAware, ISemaphoreObserver where TTarget : CCGridSelectOption {
        [SerializeField] GameObject equippedFrame;
        [SerializeField] GameObject selectedFrame;

        Prompt _promptSelect;
        
        CoyoteSemaphore _isHovered;

        public override Transform DetermineHost() => Target.ParentModel.View<VCCGridSelect>().Content;

        protected override void OnInitialize() {
            _isHovered = new CoyoteSemaphore(this);

            Refresh();
            Target.CharacterCreator.ListenTo(CharacterCreator.Events.AppearanceChanged, _ => Refresh(), this);
        }

        void Refresh() {
            equippedFrame.SetActive(Target.Index == Target.SavedValue);
            selectedFrame.SetActive(_isHovered);
        }

        void Update() {
            _isHovered.Update();
        }
        
        void OnHover() {
            if (Target.ParentModel.Data.OverrideViewTarget) {
                var viewTarget = Target.ParentModel.Data.ViewTarget;
                Target.CharacterCreator.HeroRenderer.SetViewTarget(viewTarget);
            }
            
            selectedFrame.SetActive(true);
            FMODManager.PlayOneShot(Services.Get<CommonReferences>().AudioConfig.ButtonSelectedSound);
        }

        void OnUnhover() {
            selectedFrame.SetActive(false);
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _isHovered.Notify();
                Target.CharacterCreator.SetPromptInvoker(Target);
                return UIResult.Accept;
            }
            
            if (evt is UIEMouseDown { IsLeft: true }) {
                Target.Select();
                return UIResult.Accept;
            }

            if (evt is UINaviAction naviAction) {
                if (naviAction.direction == NaviDirection.Left) {
                    Target.FocusLeft();
                } else if (naviAction.direction == NaviDirection.Right) {
                    Target.FocusRight();
                } else if (naviAction.direction == NaviDirection.Up) {
                    Target.FocusAbove();
                } else if (naviAction.direction == NaviDirection.Down) {
                    Target.FocusBelow();
                }

                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }

        void ISemaphoreObserver.OnUp() => OnHover();
        void ISemaphoreObserver.OnDown() => OnUnhover();
    }
}