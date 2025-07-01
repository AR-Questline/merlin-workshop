using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;

namespace Awaken.TG.Main.UI.Housing.UnlockHouse {
    [SpawnsView(typeof(VUnlockHousingUI))]
    public partial class UnlockHousingUI : Model, IUIStateSource, IPromptHost, IWithPricePreview, IPromptListener {
        public sealed override bool IsNotSaved => true;

        public UnlockHousingData unlockHousingData;
        readonly Action _onUnlock;
        Prompt _buyPrompt;
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public Transform PromptsHost => View<VUnlockHousingUI>().PromptsHost;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();
        
        public int Price { get; }
        public bool CanAfford { get; }

        public UnlockHousingUI(UnlockHousingData unlockHousingData, Action onUnlock) {
            this.unlockHousingData = unlockHousingData;
            _onUnlock = onUnlock;
            Price = unlockHousingData.Price;
            CanAfford = Hero.Current.Wealth >= Price;
        }

        protected override void OnFullyInitialized() {
            this.Trigger(IWithPricePreview.Events.PriceRefreshed, true);
            InitializePrompts();
        }

        void InitializePrompts() {
            var prompts = AddElement(new Prompts(this));
            _buyPrompt = prompts.AddPrompt(PopupUI.AcceptTapPrompt(BuyHouse, hold: true), this, CanAfford);
            _buyPrompt.AddListener(this);
            prompts.AddPrompt(PopupUI.CancelTapPrompt(Discard), this);
        }

        void BuyHouse() {
            Hero hero = Hero.Current;
            hero.Wealth.DecreaseBy(Price);
            _buyPrompt.SetupState(false, false);
            _onUnlock?.Invoke();
            hero.Trigger(Hero.Events.HouseBought, hero);
        }

        public static void OpenUnlockHouseUI(UnlockHousingData unlockHousingData, Action onUnlock) {
            World.Add(new UnlockHousingUI(unlockHousingData, onUnlock));
        }

        // === IPromptListener
        public void SetName(string name) { }
        public void SetActive(bool active) { }
        public void SetVisible(bool visible) { }
        
        public void OnHoldKeyUp(Prompt source, bool completed = false) {
            if (completed) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.Short);
                View<VUnlockHousingUI>().HandleBuyingSuccess();
            }
        }
    }
}