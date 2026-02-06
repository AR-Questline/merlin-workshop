using System;
using System.Linq;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    public partial class TutorialText : Model, IUIStateSource {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime();

        public string TitleText => TutorialDataOwner.GetTranslatedTitleText();
        public string ContentText => TutorialDataOwner.GetTranslatedContentText();
        public bool DisableOtherCanvases { get; }
        public ViewContext Context { get; }
        public Action CloseCallback { get; [UnityEngine.Scripting.Preserve] set; }
        ITutorialDataOwner TutorialDataOwner { get; }
        
        protected TutorialText(ITutorialDataOwner tutorialDataOwner, bool disableOtherCanvases, ViewContext viewContext) {
            TutorialDataOwner = tutorialDataOwner;
            DisableOtherCanvases = disableOtherCanvases;
            Context = viewContext;
        }
        
        public static TutorialText Show(TutorialConfig.TextTutorial dataOwner, bool disableOtherCanvases = true, ViewContext viewContext = ViewContext.Gameplay) {
            TutorialText tutorial = World.Add(new TutorialText(dataOwner, disableOtherCanvases, viewContext));
            return TryShow(tutorial, typeof(VTutorialText));
        }

        protected static T TryShow<T>(T tutorial, Type view) where T : TutorialText {
            var tutorialMaster = World.Only<TutorialMaster>();
            if (tutorialMaster.tutorialTextBuffer.Any() && tutorialMaster.tutorialTextBuffer.Last() is { } lastTutorial) {
                lastTutorial.ListenTo(Events.AfterDiscarded, ShowTutorial, tutorial);
            } else {
                ShowTutorial();
            }
            
            tutorialMaster.tutorialTextBuffer.Add(tutorial);
            return tutorial;

            void ShowTutorial() {
                World.SpawnView<VModalBlocker>(tutorial);
                var spawnedView = ((VTutorialText<T>)World.SpawnView(tutorial, view, true));
                spawnedView.Show(true);
                World.Add(new BlurBackground(tutorial, BlurConfig.WithBlurVolume)).ShowBackground(spawnedView);
            }
        }

        public void Close() {
            CloseCallback?.Invoke();
            Discard();
        }
        
        protected override void OnFullyDiscarded() {
            var tutorialMaster = World.Only<TutorialMaster>();
            tutorialMaster.tutorialTextBuffer.Remove(this);
            
            if (tutorialMaster.tutorialTextBuffer.Count == 0) {
                Services.Get<CanvasService>().ShowTutorialCanvasOnly(false);
            }
            
            base.OnFullyDiscarded();
        }

        public enum ViewContext : byte {
            Gameplay,
            Inventory
        }
    }
}