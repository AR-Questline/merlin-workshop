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

        public string TitleText { get; }
        public string ContentText { get; }
        public bool DisableOtherCanvases { get; }
        public ViewContext Context { get; }
        public Action CloseCallback { get; [UnityEngine.Scripting.Preserve] set; }
        
        protected TutorialText(string titleText, string contentText, bool disableOtherCanvases, ViewContext viewContext) {
            TitleText = titleText;
            ContentText = contentText;
            DisableOtherCanvases = disableOtherCanvases;
            Context = viewContext;
        }
        
        public static TutorialText Show(TutorialConfig.TextTutorial dataOwner, bool disableOtherCanvases = true, ViewContext viewContext = ViewContext.Gameplay) {
            TutorialText tutorial = World.Add(new TutorialText(dataOwner.title, dataOwner.text, disableOtherCanvases, viewContext));
            return TryShow(tutorial, typeof(VTutorialText));
        }

        protected static T TryShow<T>(T tutorial, Type view) where T : TutorialText {
            var tutorialMaster = World.Only<TutorialMaster>();
            if (tutorialMaster.tutorialTextBuffer.Any() && tutorialMaster.tutorialTextBuffer.Last() is { } lastTutorial) {
                lastTutorial.ListenTo(Events.AfterDiscarded, () => {
                    World.SpawnView<VModalBlocker>(tutorial);
                    ((VTutorialText<T>)World.SpawnView(tutorial, view, true)).Show(true);
                }, tutorial);
            } else {
                World.SpawnView<VModalBlocker>(tutorial);
                ((VTutorialText<T>)World.SpawnView(tutorial, view, true)).Show(true);
            }
            
            tutorialMaster.tutorialTextBuffer.Add(tutorial);
            return tutorial;
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