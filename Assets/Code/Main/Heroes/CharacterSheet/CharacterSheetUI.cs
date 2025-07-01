using System;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.TG.Debugging;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.HeroCreator;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    /// <summary>
    /// Main model for Hero Character Sheet.
    /// </summary>
    public partial class CharacterSheetUI : Model, IUIStateSource, IPromptHost, CharacterSheetTabs.ITabParent<VCharacterSheetUI> {
        static CharacterSheetTabType s_lastTab;
        Prompt _markAllAsSeenPromptKeyboard;
        Prompt _markAllAsSeenPromptGamepad;
        Prompt _escapePrompt;
        bool _useTransitionsOnExitSequence = true;

        NewThingsTracker NewThingsTracker => Services.Get<NewThingsTracker>();
        public HeroRenderer HeroRenderer { get; private set; }
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();

        public Transform TabButtonsHost => View.TabButtonsHost;
        public Transform ContentHost => View.ContentHost;
        public Transform PromptsHost => View.PromptsHost;
        public Transform MapHost => View.MapHost;
        public CharacterSheetTabType CurrentType { get; set; } = CharacterSheetTabType.Character;
        public Tabs<CharacterSheetUI, VCharacterSheetTabs, CharacterSheetTabType, ICharacterSheetTab> TabsController { get; set; }

        public CharacterSheetTabType[] OverrideAvailableTabs { get; set; }
        public Action AfterViewSpawnedCallback { get; set; }
        public Hero Hero { get; }
        public Prompts Prompts { get; private set; }
        
        VCharacterSheetUI View => View<VCharacterSheetUI>();
        public Transform OverlayLayer => View.TooltipParent;
        public Transform StaticTooltip => View.StaticTooltip;
        
        CharacterSheetUI() {
            Hero = Hero.Current;
        }
        
        protected override void OnInitialize() {
            DrakeRendererStateSystem.PushSystemFreeze();
            ++LoadingStates.PauseHlodUpdateCounter;

            World.Services.Get<FpsLimiter>().RegisterLimit(this, FpsLimiter.DefaultUIFpsLimit);
            MemoryClear.ReferencesCachesRevalidate();
        }

        protected override void OnFullyInitialized() {
            World.SpawnView<VCharacterSheetUI>(this, true);
            HeroRenderer = AddElement(new HeroRenderer(useLoadoutAnimations: true));
            World.SpawnView<VRotator>(HeroRenderer, false, true, View.RotatorHost);
            
            Prompts = AddElement(new Prompts(this));
            _markAllAsSeenPromptKeyboard = Prompt.Tap(KeyBindings.UI.Generic.MarkAllAsSeen, LocTerms.UIGenericMarkAllAsSeen.Translate(), MarkAllAsSeen, controllers: ControlSchemeFlag.KeyboardAndMouse);
            _markAllAsSeenPromptGamepad = Prompt.Hold(KeyBindings.UI.Generic.MarkAllAsSeen, LocTerms.UIGenericMarkAllAsSeen.Translate(), MarkAllAsSeen, controllers: ControlSchemeFlag.Gamepad);
            Prompts.AddPrompt(_markAllAsSeenPromptKeyboard, this);
            Prompts.AddPrompt(_markAllAsSeenPromptGamepad, this);

            var tabs = AddElement(new CharacterSheetTabs());
            _escapePrompt = Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), () => tabs.TryHandleBack(TryDiscard), Prompt.Position.Last);
            Prompts.AddPrompt(_escapePrompt, this);
            
            Hero.ListenTo(Events.AfterChanged, TriggerChange, this);
            Hero.HeroItems.ListenTo(HeroLoadout.Events.FailedToChangeLoadout, () => {
                PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI),
                    LocTerms.FailedToEquipItemToLoadout.Translate(),
                    LocTerms.LoadoutEquipBlocked.Translate());
            }, this);
        }

        protected override void OnFullyDiscarded() {
            DrakeRendererStateSystem.PopSystemFreeze();
            --LoadingStates.PauseHlodUpdateCounter;
            ExitSequence().Forget();
        }

        public static void ToggleCharacterSheet() {
            var ui = ToggleCharacterSheet(s_lastTab ?? CharacterSheetTabType.Inventory);
            if (ui == null) {
                return;
            }
            
            if (ui.TryGetElement<ICharacterSheetTabWithSubTabs>(out var tabWithSubTabs)) {
                tabWithSubTabs.TryToggleSubTab(ui);
            }
        }
        
        public static CharacterSheetUI ToggleCharacterSheet(CharacterSheetTabType initialTab, bool ignoreMapState = false, CharacterSheetTabType[] availableTabs = null, Action afterViewSpawnedCallback = null) {
            CharacterSheetUI sheet = World.Any<CharacterSheetUI>();
            if (sheet != null) {
                if (sheet.HeroRenderer.IsLoading == false) {
                    sheet.Discard();
                }
            } else if (ignoreMapState || UIStateStack.Instance.State.IsMapInteractive) {
                return World.Add(new CharacterSheetUI { CurrentType = initialTab, OverrideAvailableTabs = availableTabs, AfterViewSpawnedCallback = afterViewSpawnedCallback });
            }
            return null;
        }
        
        public static void ChangeLastTab(CharacterSheetTabType tab) {
            s_lastTab = tab;
        }
        
        public void SetRendererTarget(HeroRenderer.Target target) {
            HeroRenderer.SetViewTarget(target);
        }
        
        public void SetRendererTargetInstant(HeroRenderer.Target target) {
            HeroRenderer.SetViewTargetInstant(target);
        }
        
        public void SetRendererTarget(EquipmentSlotType target) {
            HeroRenderer.SetViewTarget(target);
        }
        
        public void SetHeroOnRenderVisible(bool visibility) {
            HeroRenderer.SetHeroVisibility(visibility);
            if (visibility) {
                FadeOutForegroundQuad().Forget();
            }
        }
        
        public void SetMarkAllAsSeenPromptActive(bool active) {
            bool state = active && NewThingsTracker.HasAnyThingsToMarkAsSeen();
            _markAllAsSeenPromptKeyboard.SetupState(state, state);
            _markAllAsSeenPromptGamepad.SetupState(state, state);
        }

        public void TryDiscard() {
            if (HeroRenderer.IsLoading) {
                return;
            }
            Discard();
        }

        public void DiscardWithoutTransition() {
            _useTransitionsOnExitSequence = false;
            Discard();
        }
        
        async UniTaskVoid FadeOutForegroundQuad() {
            HeroRenderer.ShowForegroundQuad();
            if (await AsyncUtil.WaitUntil(this, () => !HeroRenderer.IsLoading)) {
                HeroRenderer.FadeForegroundQuad(0f, 0.5f, 0.2f);
            }
        }
        
        void MarkAllAsSeen() {
            NewThingsTracker.MarkAllAsSeen();
            _markAllAsSeenPromptKeyboard.SetupState(false, false);
            _markAllAsSeenPromptGamepad.SetupState(false, false);
        }

        async UniTaskVoid ExitSequence() {
            if (_useTransitionsOnExitSequence) {
                World.Services.Get<TransitionService>().SetToBlack();
            }
#if !UNITY_EDITOR
            await UniTask.DelayFrame(1);
            // Game is made of invalid states, so GC.Collect (which should be called here) has a high chance to crash
            // So to make it less likely to crash, we don't clear memory manually
            await Resources.UnloadUnusedAssets();
            await UniTask.DelayFrame(2);
#endif
            if (_useTransitionsOnExitSequence) {
                await World.Services.Get<TransitionService>().ToCamera(TransitionService.QuickFadeOut, 0.2f);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            s_lastTab = CurrentType;
        }
    }
}