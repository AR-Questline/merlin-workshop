using System;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    public partial class Credits : Model, IUIStateSource, IPromptHost {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;
        
        public UIState UIState => UIState.ModalState(HUDState.EverythingHidden);
        public Transform PromptsHost => View<VCredits>().PromptsHost;
        
        public static UniTask Show(Type viewType) {
            var credits = World.Add(new Credits());
            World.SpawnView<VModalBlocker>(credits);
            World.SpawnView(credits, viewType, true);
            
            if (World.Any<AllSettingsUI>() is { } settingsUI) {
                var vSettingsUI = settingsUI.View<VSettingsUI>();
                UIUtils.AddOverlayUIView(credits, vSettingsUI);
            }
            return AsyncUtil.WaitForDiscard(credits);
        }

        public void InitPrompts(VCredits vCredits) {
            var prompts = AddElement(new Prompts(this));
            var skipPrompt = Prompt.Hold(KeyBindings.UI.Items.SelectItem, string.Empty, Discard);
            prompts.BindPrompt(skipPrompt, this, vCredits.SkipPrompt);
        }
    }
}