using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools;
using Awaken.TG.Debugging.ModelsDebugs.Runtime;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.Crafting.AlchemyCrafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.TG.Main.Settings.Debug;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.FrameTimings;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using Awaken.Utility.UI;
using QFSW.QC;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using Log = Awaken.Utility.Debugging.Log;
using Volume = UnityEngine.Rendering.Volume;

namespace Awaken.TG.Debugging.Cheats {
    public sealed partial class MarvinMode : Model {
        public override Domain DefaultDomain => Domain.Gameplay;

        public override bool IsNotSaved => true;

        public string SearchModelId { get; private set; }
        
        List<GameObject> _disabledUI = new();
        Hero Hero => Hero.Current;

        public static MarvinMode GetOrCreateMarvin() {
            if (World.Any<MarvinMode>() is { } mm) {
                return mm;
            }

            MarvinMode orCreateMarvin = World.Add(new MarvinMode());
            orCreateMarvin.ToggleView();
            return orCreateMarvin;
        }

        protected override void OnInitialize() {
            ToggleView();
        }
        
        [UnityEngine.Scripting.Preserve]
        public void FindModelId() {
            SearchModelId = MarvinUtils.ThrowRaycastToFindModelId();
        }

        public void ToggleView() {
            if (MainView) {
                MainView.Discard();
                UIStateStack.Instance.ReleaseAllOwnedBy(this);
            } else {
                var uiState = UIState.ModalState(HUDState.None);
                UIStateStack.Instance.PushState(uiState, this);
                World.SpawnView<VMarvinMode>(this, true);
            }
        }

        public static void HideView() {
            var marvin = World.Any<MarvinMode>();
            if (marvin && marvin.MainView) {
                marvin.ToggleView();
            }
        }

        public static void ShowView() {
            var marvin = World.Any<MarvinMode>();
            if (!marvin) {
                World.Add(new MarvinMode());
            } else if (!marvin.MainView) {
                marvin.ToggleView();
            }
        }

        [MarvinButton]
        void ShowModelsDebug() {
            if (!World.HasAny<ModelsDebugModel>()) {
                World.Add(new ModelsDebugModel());
            }
        }
        
        [MarvinButton]
        void AddHeroHP(float value) {
            if (Hero) {
                Hero.Health.IncreaseBy(value);
            }
        }
        
        [MarvinButton]
        void AddHeroMaxHP(float value) {
            if (Hero) {
                Hero.MaxHealth.IncreaseBy(value);
            }
        }
        
        [MarvinButton]
        void TeleportTo(string position) {
            if (string.IsNullOrEmpty(position)) {
                return;
            }
            
            Regex regex = new(@"[-+]?\d+(?:\.\d+)?");
            MatchCollection matches = regex.Matches(position);
            
            if (float.TryParse(matches[0].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(matches[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(matches[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float z) && Hero) {
                Hero.TeleportTo(new Vector3(x, y, z));
            }
        }

        // === Debug Stats
        [MarvinButton(state: nameof(IsDebugStatsUIActive))]
        void ToggleDebugStatsUI() {
            var heroDebugStats = World.Any<DebugStatsUI>();
            if (heroDebugStats != null) {
                heroDebugStats.Discard();
                return;
            }
            
            World.Add(new DebugStatsUI());
        }
        
        [Command("toggle.debug-stats-ui", "Toggles Debug Stats UI")][UnityEngine.Scripting.Preserve]
        static void DebugStatsUICommand() {
            GetOrCreateMarvin().ToggleDebugStatsUI();
        }
        
        bool IsDebugStatsUIActive() => World.HasAny<DebugStatsUI>();

        // === Invisibility
        [Command("toggle.invisibility", "Toggles Invisibility")][UnityEngine.Scripting.Preserve]
        static void InvisibilityCommand() {
            GetOrCreateMarvin().ToggleHeroInvisibility();
        }

        [MarvinButton(state: nameof(IsInvisibilityActive))]
        bool ToggleHeroInvisibility() {
            if (!Hero) return false;
            HeroDebugInvisibility invisibility = Hero.TryGetElement<HeroDebugInvisibility>();
            if (invisibility == null) {
                Hero.AddElement<HeroDebugInvisibility>();
                return true;
            }
            invisibility.Discard();
            return false;
        }

        bool IsInvisibilityActive() => Hero?.HasElement<HeroDebugInvisibility>() ?? false;
        
        // === Immortality

        [MarvinButton(state: nameof(IsHeroImmortal))]
        bool ToggleImmortality() {
            if (!Hero) return false;
            if (!QCGameplayTools.ImmortalityEnabled) {
                ToggleGodModeOn();
                _heroWasImmortal = true; //Overrides saved value so that the new state is kept irrespective of other systems
                return true;
            }
            
            ToggleGodModeOff();
            _heroWasImmortal = false; //Overrides saved value so that the new state is kept irrespective of other systems
            return false;
        }

        // For reapplying immortality state after exiting no clip
        bool _heroWasImmortal;
        void ToggleGodModeOn() {
            _heroWasImmortal = IsHeroImmortal();
            QCGameplayTools.EnableImmortality();
        }
        void ToggleGodModeOff() {
            _heroWasImmortal = IsHeroImmortal();
            QCGameplayTools.DisableImmortality();
        }

        bool IsHeroImmortal() {
            return QCGameplayTools.ImmortalityEnabled;
        }
        
        // === NoClip
        
        [Command("toggle.no-clip", "Toggles NoClip")][UnityEngine.Scripting.Preserve]
        static void NoClipCommand() {
            GetOrCreateMarvin().ToggleNoClip();
        } 
        
        [MarvinButton(state: nameof(IsNoClipActive))]
        void ToggleNoClip() {
            if (!Hero) return;

            //Toggle On
            if (!IsNoClipActive()) {
                Hero.TrySetMovementType<NoClipMovement>();
                
                ToggleGodModeOn();
                return;
            }

            //Toggle Off
            Hero.ReturnToDefaultMovement();
            
            if (!_heroWasImmortal && IsHeroImmortal()) {
                ToggleGodModeOff();
            }
        }

        bool IsNoClipActive() {
            return Hero?.MovementSystem is NoClipMovement;
        }
        
        //Fishing
        [MarvinButton(state: nameof(IsDebugFishingActive))]
        void ToggleDebugFishing() {
            DebugFishing debugFishing = Services.TryGet<DebugFishing>();
            if (debugFishing == null) {
                return;
            }
            
            if (!debugFishing.IsEnabled) {
                debugFishing.Enable();
            } else {
                debugFishing.Disable();
            }
        }
        
        bool IsDebugFishingActive() {
            return Services.TryGet(out DebugFishing debugFishing) && debugFishing.IsEnabled;
        }
        
        // AI Debugging
        [MarvinButton(state: nameof(IsDebugAIActive))]
        void ToggleDebugAI() {
            DebugAI debugAI = Services.TryGet<DebugAI>();
            if (debugAI == null) {
                return;
            }
            
            //Toggle
            if (!IsDebugAIActive()) {
                debugAI.Enable();
            } else {
                debugAI.Disable();
            }
        }

        bool IsFastReloadingEnabled() {
            if (!Services.TryGet(out SceneService sceneService) || 
                sceneService.MainSceneBehaviour == null || 
                sceneService.MainSceneBehaviour is not SubdividedScene subdividedScene) {
                return false;
            }

            return subdividedScene.EnableFastReloading;
        }
        
        [MarvinButton(state: nameof(IsFastReloadingEnabled))]
        void ToggleEnableFastReloading() {
            var subdividedScene = Object.FindAnyObjectByType<SubdividedScene>();
            if (subdividedScene == null) {
                return;
            }
            subdividedScene.EnableFastReloading = !subdividedScene.EnableFastReloading;
        }

        bool IsDebugAIActive() {
            return Services.TryGet<DebugAI>()?.IsEnabled ?? false;
        }
        
        [MarvinButton(visible: nameof(IsDebugAIActive), state: nameof(DebugAIStatsState))]
        void ToggleDebugAIStats() {
            if (Services.TryGet(out DebugAI debugAI)) {
                debugAI.ToggleStats();
            }
        }
        bool DebugAIStatsState() => Services.TryGet(out DebugAI debugAI) && debugAI.ShowStats;
        
        [MarvinButton(visible: nameof(IsDebugAIActive), state: nameof(DebugAIStateState))]
        void ToggleDebugAIState() {
            if (Services.TryGet(out DebugAI debugAI)) {
                debugAI.ToggleState();
            }
        }
        bool DebugAIStateState() => Services.TryGet(out DebugAI debugAI) && debugAI.ShowState;
        
        [MarvinButton(visible: nameof(IsDebugAIActive), state: nameof(DebugAITargetingState))]
        void ToggleDebugAITargeting() {
            if (Services.TryGet(out DebugAI debugAI)) {
                debugAI.ToggleTargeting();
            }
        }
        bool DebugAITargetingState() => Services.TryGet(out DebugAI debugAI) && debugAI.ShowTargeting;

        [MarvinButton(visible: nameof(IsDebugAIActive), state: nameof(DebugAITurningState))]
        void ToggleDebugAITurning() {
            if (Services.TryGet(out DebugAI debugAI)) {
                debugAI.ToggleTurning();
            }
        }
        bool DebugAITurningState() => Services.TryGet(out DebugAI debugAI) && debugAI.ShowTurning;
        
        [MarvinButton(visible: nameof(IsDebugAIActive), state: nameof(DebugAIAnimationsState))]
        void ToggleDebugAIAnimations() {
            if (Services.TryGet(out DebugAI debugAI)) {
                debugAI.ToggleDebugAnimations();
            }
        }
        bool DebugAIAnimationsState() => Services.TryGet(out DebugAI debugAI) && debugAI.ShowAnimations;
        
        [MarvinButton(visible: nameof(IsDebugAIActive), state: nameof(DebugAIAlertAndAggressionState))]
        void ToggleDebugAIAlertAndAggression() {
            if (Services.TryGet(out DebugAI debugAI)) {
                debugAI.ToggleAlertAndAggression();
            }
        }
        bool DebugAIAlertAndAggressionState() => Services.TryGet(out DebugAI debugAI) && debugAI.ShowAlertAndAggression;
        
        [MarvinButton(visible: nameof(IsDebugAIActive), state: nameof(DebugAIThieveryState))]
        void ToggleDebugAIThievery() {
            if (Services.TryGet(out DebugAI debugAI)) {
                debugAI.ToggleThievery();
            }
        }
        bool DebugAIThieveryState() => Services.TryGet(out DebugAI debugAI) && debugAI.ShowThievery;
        
        // Npc Historian
        
        [MarvinButton(state: nameof(NpcHistorianState))]
        void ToggleNpcHistorian() => NpcHistorian.Enabled = !NpcHistorian.Enabled;
        bool NpcHistorianState() => NpcHistorian.Enabled;
        
        // Location Debugging

        [MarvinButton(state: nameof(DebugLocationState))]
        void ToggleDebugLocation() {
            var debug = World.Any<DebugLocation>();
            if (debug) {
                debug.Discard();
            } else {
                World.Add(new DebugLocation());
            }
        }
        bool DebugLocationState() => World.HasAny<DebugLocation>();

        [MarvinButton]
        void UnlockGameplayTalents() {
            if (!Hero) return;

            Hero.TryGetElement<HeroDevelopment>()?.UnlockGameplayTalents();
        }

        [MarvinButton(state: nameof(FrameTimingsEnabled))]
        void ToggleFrameTimings() {
            FrameTimingHUDDisplay.Toggle(UGUIWindowUtils.WindowPosition.BottomLeft);
        }
        
        bool FrameTimingsEnabled() => FrameTimingHUDDisplay.IsShown;

        [MarvinButton(state: nameof(FrameStatsEnabled))]
        void ToggleFrameStats() {
            FrameStatsWindow.Toggle(UGUIWindowUtils.WindowPosition.BottomLeft);
        }

        bool FrameStatsEnabled() => FrameStatsWindow.IsShown;
        
        // === Other
        [MarvinButton]
        public void ShowUI() {
            _disabledUI.ForEach(go => {
                if (go) {
                    go.SetActive(true);
                }
            });
            _disabledUI.Clear();

            ShowUTK();
        }
        
        [MarvinButton]
        public void HideUI() {
            List<Type> withoutTypes = new();
            HideUIWithout(withoutTypes);
        }
        
        void HideUIWithout(List<Type> withoutTypes) {
            World.EventSystem.RemoveAllListenersOwnedBy(this);
            
            foreach (Type withoutType in withoutTypes) {
                World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded(withoutType), this, () => HideUIImpl(withoutTypes));
            }
            
            HideUIImpl(withoutTypes);
        }

        void HideUIImpl(List<Type> withoutTypes) {
            ShowUI();

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(static c => {
                    if (c.transform.parent) {
                        return c.transform.parent.GetComponentInParent<Canvas>() == null;
                    }
                    return true;
                })
                .ToArray();
            var fullHideCanvases = canvases.Where(c => withoutTypes.All(t => c.GetComponentInChildren(t) == null))
                .ToArray();
            fullHideCanvases.ForEach(c => {
                var go = c.gameObject;
                go.SetActive(false);
                _disabledUI.Add(go);
            });
            var mixedHideCanvases = canvases.Except(fullHideCanvases).ToArray();
            
            var canNotHide = mixedHideCanvases
                .SelectMany(c =>
                    withoutTypes.SelectMany(t => c.GetComponentsInChildren(t).Select(com => com.gameObject)))
                .SelectMany(ListParentToCanvas)
                .ToHashSet();

            Queue<GameObject> queue = new();
            mixedHideCanvases.ForEach(c => {
                foreach (Transform child in c.transform) {
                    queue.Enqueue(child.gameObject);
                }
            });

            while (queue.Count > 0) {
                var child = queue.Dequeue();
                if (withoutTypes.Any(t => child.GetComponent(t))) {
                    continue;
                }
                if (canNotHide.Contains(child)) {
                    foreach (Transform c2 in child.transform) {
                        queue.Enqueue(c2.gameObject);
                    }
                } else {
                    if (child.activeSelf) {
                        child.SetActive(false);
                        _disabledUI.Add(child);
                    }
                }
            }

            HideUTK();
        }

        void HideUTK() {
            SetUTKActive(false);
        }
        
        void ShowUTK() {
            SetUTKActive(true);
        }

        void SetUTKActive(bool state) {
            var hudDocument = World.Services.Get<UIDocumentProvider>().TryGetDocument(UIDocumentType.HUD);
            var defaultDocument = World.Services.Get<UIDocumentProvider>().TryGetDocument(UIDocumentType.Default);
            
            SetDocumentActive(hudDocument, state);
            SetDocumentActive(defaultDocument, state);
        }
        
        void SetDocumentActive(UIDocument document, bool state) {
            document.rootVisualElement.SetActiveOptimized(state);
        }
        
        static IEnumerable<GameObject> ListParentToCanvas(GameObject canvasChild) {
            var currentParent = canvasChild.transform.parent;
            while (currentParent.gameObject.GetComponent<Canvas>() == null) {
                yield return currentParent.gameObject;
                currentParent = currentParent.parent;
            }
        }

        [MarvinButton]
        void AddItems(int count = 1, bool onlyExistingInGame = true) {
            var items = Hero?.HeroItems;
            if (!items) {
                return;
            }

            bool bufferWasBlocked = AdvancedNotificationBuffer.AllNotificationsSuspended;
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            try {
                Hero.HeroStats.EncumbranceLimit.SetTo(1000000);
                if (onlyExistingInGame) {
                    foreach (var item in ItemsInGameCache.Get.allItemsInGame) {
                        item.Get<ItemTemplate>().ChangeQuantity(items, count);
                    }
                } else {
                    World.Services.Get<TemplatesProvider>()
                        .GetAllOfType<ItemTemplate>()
                        .ForEach(t => t.ChangeQuantity(Hero.Inventory, count));
                }
            } finally {
                AdvancedNotificationBuffer.AllNotificationsSuspended = bufferWasBlocked;
            }
        }
        
        [MarvinButton]
        void AddSketchingTool() {
            var items = Hero?.HeroItems;
            if (!items) {
                return;
            }

            bool bufferWasBlocked = AdvancedNotificationBuffer.AllNotificationsSuspended;
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            try {
                Hero.HeroStats.EncumbranceLimit.SetTo(100);
                World.Services.Get<TemplatesProvider>()
                    .GetAllOfType<ItemTemplate>()
                    .First((t) => t.itemName.IdOverride == "item_weapon_2h_sketchbook")
                    .ChangeQuantity(Hero.Inventory, 1);
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AdvancedNotificationBuffer.AllNotificationsSuspended = bufferWasBlocked;
            }
        }

        [MarvinButton, Command("once.learn-all-recipes", "Hero learns all recipes")]
        static void LearnAllRecipes() {
            if (!Hero.Current) return;

            bool bufferWasBlocked = AdvancedNotificationBuffer.AllNotificationsSuspended;
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            try {
                var recipes = Hero.Current.Element<HeroRecipes>();
                var provider = World.Services.Get<TemplatesProvider>();

                foreach (var recipe in provider.GetAllOfType<AlchemyRecipe>()) {
                    recipes.LearnRecipe(recipe);
                }
                foreach (var recipe in provider.GetAllOfType<CookingRecipe>()) {
                    recipes.LearnRecipe(recipe);
                }
                foreach (var recipe in provider.GetAllOfType<HandcraftingRecipe>()) {
                    recipes.LearnRecipe(recipe);
                }
            } finally {
                AdvancedNotificationBuffer.AllNotificationsSuspended = bufferWasBlocked;
            }
        }
        
        [MarvinButton, Command("day-night.set-day-duration", "Sets day duration in minutes")]
        public static void SetWeatherDayDuration(float minutes) {
            var lightingWeather = Object.FindAnyObjectByType<TimeOfDayPostProcessesController>();
            if (lightingWeather) {
                var currentMinutes = 24f*60f/World.Only<GameRealTime>().WeatherSecondsPerRealSecond;
                var multiplier = currentMinutes/minutes;
                
                var volumes = lightingWeather.GetComponentsInChildren<Volume>(true);
                var profiles = volumes.Select(v => v.GetSharedOrInstancedProfile()).WhereNotNull();
                var visualEnvironments = profiles.Where(p => p.Has<VisualEnvironment>())
                    .Select(p => p.components.OfType<VisualEnvironment>().First());
                visualEnvironments.ForEach(ve => ve.windSpeed.value *= multiplier);
            }
            World.Only<GameRealTime>().SetWeatherDayDuration(minutes);
        }
        
        [MarvinButton]
        void DiscardAllNpcs() {
            while (World.HasAny<NpcElement>()) {
                World.Any<NpcElement>().ParentModel.Discard();
            }
        }

        [MarvinButton]
        void ForceEndStory() {
            foreach (var story in World.All<Story>().Where(s => s.InvolveHero).ToList()) {
                StoryUtils.EndStory(story);
            }
        }

        [MarvinButton(state: nameof(IsSecondaryDebugEnabled))]
        void SecondaryDebugKeyBindings() {
            GlobalKeys.secondaryDebugActions = !GlobalKeys.secondaryDebugActions;
        }

        bool IsSecondaryDebugEnabled() => GlobalKeys.secondaryDebugActions;

        [MarvinButton(state: nameof(IsRuntimeRenderingDebuggerOn))]
        void DisplayRuntimeRenderingDebugger() {
            var nextState = !IsRuntimeRenderingDebuggerOn();
            if (nextState) {
                // HACK: This enables frame stats which are required by rendering debugger
                FrameTimingHUDDisplay.Toggle(UGUIWindowUtils.WindowPosition.Center);
            }
            DebugManager.instance.enableRuntimeUI = nextState;
            DebugManager.instance.displayRuntimeUI = nextState;
        }

        bool IsRuntimeRenderingDebuggerOn() => DebugManager.instance.enableRuntimeUI;
        
        [MarvinButton, Command("toggle.flickerfix", "Toggles fix for flickering screen on some older GPUs, that caps certain post process effects values")]
        static void ToggleFlickerFix() {
            var flickerFix = World.Only<FlickerFixSetting>();
            flickerFix.Option.Enabled = !flickerFix.Enabled;
            Log.Important?.Info($"FlickerFix: {(flickerFix.Enabled ? "Enabled" : "Disabled")}");
        }
        
        [MarvinButton, Command("template.spawn-all-unique-npcs-to-abyss", "Spawns all unique NPCs to Abyss so they are ready to be used and are findable by all systems, even if they were never met by player")]
        static void SpawnAllUniqueNPCs() {
            var templates = World.Services.Get<TemplatesProvider>().GetAllOfType<LocationTemplate>()
                .Where(l => l.TryGetComponent<UniqueNpcAttachment>(out _));
            foreach (var temp in templates) {
                if (World.Services.Get<NpcRegistry>().TryGetNpc(temp, out var npc)) {
                    continue;
                }
                var location = temp.SpawnLocation(NpcPresence.AbyssPosition, Quaternion.identity, temp.transform.localScale);

                location.MoveToDomain(Domain.Gameplay);
                location.SetInteractability(LocationInteractability.Hidden);
                
                npc = location.Element<NpcElement>();
                if (npc.ParentModel.TryGetElement(out IdleDataElement data)) {
                    Log.Important?.Error($"Npc ({npc}) with unique presence cannot have IdleDataAttachment!", npc.ParentModel.Spec);
                    data.Discard();
                }
            }
        }

        [MarvinButton(state: nameof(IsImmediateStoryActive)), Command("toggle.immediate-story", "Toggles immediate story mode")]
        static void ToggleImmediateStory() {
            DebugReferences.ImmediateStory = !DebugReferences.ImmediateStory;
        }

        static bool IsImmediateStoryActive() => DebugReferences.ImmediateStory;

        [MarvinButton, Command("set.performance-weather", "Sets weather for performance testing")]
        static void SetPerformanceWeather() {
            SetPerformanceWeather(13, 0);
        }

        public static void SetPerformanceWeather(int hour, int minute) {
            MarvinMode.SetWeatherDayDuration(9999999);
            var gameRealTime = World.Only<GameRealTime>();
            gameRealTime.Element<WeatherController>().StopPrecipitation(true);
            gameRealTime.SetWeatherTime(hour, minute);
            var blocker = World.Add(new TimeBlocker("Performance weather", TimeType.Weather));
            gameRealTime.ListenTo(Events.AfterDiscarded, () => blocker.Discard(), blocker);
        }
        
        [MarvinButton(state: nameof(PrecipitationController.ForceRain)), Command("toggle.rain", "Toggles rain")] [UnityEngine.Scripting.Preserve]
        static void ToggleRain() {
            PrecipitationController.ForceRain = !PrecipitationController.ForceRain;
        }

        // Commands
        [Command("cycle.post-processing", "Cycles preset post processing profiles")][UnityEngine.Scripting.Preserve]
        static void CyclePostProcessing() {
            if (PostProcessingCycler.TotalPostProcesses <= 0) return;
            PostProcessingCycler.alternatePostProcessingMode = ++PostProcessingCycler.alternatePostProcessingMode % PostProcessingCycler.TotalPostProcesses;
        }

        [Command("ui.hero-storage", "Opens hero storage")][UnityEngine.Scripting.Preserve]
        static void OpenStorage() {
            Hero.Current.Storage.Open();
        }

        [Command("jobs.set-workers", "Sets job workers count")][UnityEngine.Scripting.Preserve]
        static void SetJobWorkersCount(int count) => JobsUtility.JobWorkerCount = count;

        [Command("jobs.reset-workers", "Resets job workers count")][UnityEngine.Scripting.Preserve]
        static void ResetJobWorkersCount() => JobsUtility.ResetJobWorkerCount();

        [Command("items.collect-from-map", "Collects items from map")][UnityEngine.Scripting.Preserve]
        static void CollectItemsFromMap(int count = 52) {
            var hero = Hero.Current;
            var heroPosition = hero.Coords;
            using var pickActions = World.All<PickItemAction>().GetManagedEnumerator();
            var pickups = pickActions.OrderBy(p => (heroPosition-p.ParentModel.Coords).sqrMagnitude).ToArray();
            count = Mathf.Min(count, pickups.Length);
            for (var i = 0; i < count; i++) {
                pickups[i].StartInteraction(hero, pickups[i].ParentModel);
            }
        }

        [Command("toggle.distance-culler-gui", "Toggles distance culler gui")][UnityEngine.Scripting.Preserve]
        static void ToggleDistanceCullerGUI() {
            if (World.Services.Get<DistanceCullersService>().TryGetAny(out var distanceCuller)) {
                distanceCuller.ShowGUI = !distanceCuller.ShowGUI;
            }
        }

        [Command("toggle.distance-culler", "Toggles distance culler")][UnityEngine.Scripting.Preserve]
        static void ToggleDistanceCullerActive() {
            if (World.Services.Get<DistanceCullersService>().TryGetAny(out var distanceCuller)) {
                distanceCuller.enabled = !distanceCuller.enabled;
            }
        }

        [Command("debug.crash", "Crashes the game")][UnityEngine.Scripting.Preserve]
        static void Crash() {
            DebugUtils.Crash();
        }
    }
}