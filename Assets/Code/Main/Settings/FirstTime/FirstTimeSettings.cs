using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Settings.FirstTime {
    [SpawnsView(typeof(VFirstTimeSettings))]
    public partial class FirstTimeSettings : Model, ISettingHolder, IPromptHost, IUIStateSource {
        public sealed override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.Globals;
        public Transform PromptsHost => View.PromptsHost;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();
        
        public Prompts Prompts => Element<Prompts>();
        VFirstTimeSettings View => View<VFirstTimeSettings>();
        
        Prompt _confirm;
        Prompt _restore;
        
        protected override void OnInitialize() {
            AddElement(new Prompts(this));
        }

        protected override void OnFullyInitialized() {
            InitPrompts();
        }
        
        void InitPrompts() {
            _confirm = Prompt.Tap(KeyBindings.UI.Settings.ApplyChanges, LocTerms.Confirm.Translate(), TryConfirm, Prompt.Position.Last);
            _restore = Prompt.Tap(KeyBindings.UI.Settings.RestoreDefaults, LocTerms.UITalentsReset.Translate(), ResetToDefaults, Prompt.Position.Last);
            
            Prompts.AddPrompt(_confirm, this, PromptsHost).AddAudio();
            Prompts.AddPrompt(_restore, this, PromptsHost).AddAudio();
        }
        
        void TryConfirm() {
            Reference<PopupUI> popup = new();
            popup.item = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupConfirmApplyingChanges.Translate(),
                PopupUI.AcceptTapPrompt(Confirm),
                PopupUI.CancelTapPrompt(() => {
                    popup.item.Discard();
                }),
                LocTerms.Confirm.Translate()
            );
            return;

            void Confirm() {
                ApplySettings();
                popup.item.Discard();
                DiscardAfterFadeOut().Forget();
            }
        }

        void ApplySettings() {
            foreach (var sTuple in View.Settings) {
                sTuple.setting.Apply(out _);
            }
            
            PrefMemory.Save();
        }
        
        void ResetToDefaults() {
            foreach (var sTuple in View.Settings) {
                sTuple.setting.RestoreDefault();
            }
        }

       async UniTask DiscardAfterFadeOut() {
            PrefMemory.Set(TitleScreenUtils.FirstTimeSettingPrefKey, true, false);
            var transition = World.Services.Get<TransitionService>();
            await transition.ToBlack(TransitionService.QuickFadeIn);
            Discard();
        }
    }
}
