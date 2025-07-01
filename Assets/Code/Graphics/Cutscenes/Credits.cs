using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VCredits))]
    public partial class Credits : Model, IUIStateSource, IPromptHost {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;
        
        public UIState UIState => UIState.ModalState(HUDState.EverythingHidden);
        public Transform PromptsHost => View<VCredits>().PromptsHost;
        Prompts Prompts => Element<Prompts>();

        protected override void OnInitialize() {
            AddElement(new Prompts(this));
        }

        protected override void OnFullyInitialized() {
            InitPrompts();
        }

        void InitPrompts() {
            var exitPrompt = Prompt.Hold(KeyBindings.UI.Generic.Exit, LocTerms.Exit.Translate(), Discard);
            Prompts.AddPrompt(exitPrompt, this, PromptsHost);
        }
    }
}