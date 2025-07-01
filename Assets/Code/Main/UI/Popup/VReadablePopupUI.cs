using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Popup {
    [UsesPrefab("Story/VReadablePopupUI")]
    public class VReadablePopupUI : VMediumPopupUI<Story>, IVStoryPanel {
        [Space]
        [SerializeField] LayoutElement textLayout;
        [SerializeField] LayoutElement artLayout;
        [Space]
        [SerializeField] protected VGenericPromptUI closeButton;
        [SerializeField] VGenericPromptUI takeButton;
        [SerializeField] VGenericPromptUI stealButton;
        [SerializeField] public EventReference closeSound;

        protected Prompts _prompts;
        protected Prompt _closePrompt;
        Prompt _takePrompt;
        Prompt _stealPrompt;

        public override void OfferChoice(ChoiceConfig choiceConfig) { }
        protected override void OnInitialize() {
            base.OnInitialize();
            textLayout?.gameObject.SetActive(false);
            artLayout?.gameObject.SetActive(false);
            UIStateStack.Instance.PushState(UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime(), Target);
            Target.AddElement(new StoryOnTop());
        }

        protected override void OnFullyInitialized() {
            InitPrompts();

            if (Target.Item != null) {
                SetupItemReadable();
            } else if (Target.FocusedLocation != null) {
                SetupLocationReadable();
            }

            FMODManager.PlayOneShot(closeSound);
        }

        protected virtual void InitPrompts() {
            _prompts = Target.AddElement(new Prompts(null));
            _closePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), OnClose), Target, closeButton);
            _takePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Items.TakeItem, LocTerms.Pickup.Translate(), OnTake), Target, takeButton);
            _stealPrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Items.TakeItem, LocTerms.Steal.Translate(), OnSteal), Target, stealButton);
            
            _closePrompt.SetVisible(true);
            _takePrompt.SetVisible(false);
            _stealPrompt.SetVisible(false);

            _closePrompt.AddAudio(new PromptAudio {
                TapSound = closeSound
            });
        }

        void SetupItemReadable() {
            SetTitle(Target.Item.DisplayName);
            Hero.Current.Element<HeroReadables>().RegisterTemplateAsRead(Target.Item.Template);

            if (Target.Item.TryGetElement(out ItemBeingPicked picked)) {
                if (Crime.Theft(Target.Item, picked).IsCrime()) {
                    _stealPrompt?.SetVisible(true);
                } else {
                    _takePrompt?.SetVisible(true);
                }
            }
        }

        void SetupLocationReadable() {
            SetTitle(Target.FocusedLocation.DisplayName);
        }
        
        public override void ShowText(TextConfig textConfig) {
            base.ShowText(textConfig);
            textLayout?.gameObject.SetActive(true);
        }

        public override void SetArt(SpriteReference art) {
            if (art == null || art.IsSet == false) {
                artLayout?.gameObject.SetActive(false);
                base.SetArt(null);
                return;
            }

            artLayout?.gameObject.SetActive(true);
            base.SetArt(art);
        }

        public override void ShowChange(Stat changedStat, int change) { }

        public override void Clear() {
            GameObjects.DestroyAllChildrenSafely(parent, textPrefab);
            _takePrompt?.SetVisible(false);
            _stealPrompt?.SetVisible(false);
            textLayout?.gameObject.SetActive(false);
            artLayout?.gameObject.SetActive(false);
        }

        public void ClearText() {
            textLayout?.gameObject.SetActive(false);
        }

        protected void OnClose() {
            if (Target.Item != null && Target.Item.HasElement<ItemBeingPicked>()) { 
                // If read from a location. cleanup item
                Target.Item.Discard();
            }
            Target.FinishStory();
        }
        
        void OnTake() {
            if (Target.Item != null && Target.Item.TryGetElement(out ItemBeingPicked picked)) {
                Target.Item.MoveTo(Hero.Current.Inventory);
                picked.DiscardSource();
            }
            Target.FinishStory();
        }

        void OnSteal() {
            if (Target.Item != null && Target.Item.TryGetElement(out ItemBeingPicked picked)) {
                CommitCrime.Theft(Target.Item, picked);
                Target.Item.MoveTo(Hero.Current.Inventory);
                picked.DiscardSource();
            }
            Target.FinishStory();
        }
    }
}