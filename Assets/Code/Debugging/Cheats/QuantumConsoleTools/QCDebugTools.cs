using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Graphics;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.WyrdStalker;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Saving.Cloud.Conflicts;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using QFSW.QC;
using UnityEngine;
using Log = Awaken.Utility.Debugging.Log;
using Object = UnityEngine.Object;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCDebugTools {
        [Command("toggle.raw-damage-logs", "Logs math behind damage calculations and all applied modifiers")][UnityEngine.Scripting.Preserve]
        static void DamageCalculationsLogs() {
            RawDamageData.showCalculationLogs = !RawDamageData.showCalculationLogs;
            
            QuantumConsole.Instance.LogToConsoleAsync("Raw damage logs: " + (RawDamageData.showCalculationLogs ? "enabled" : "disabled"));
        }

        [Command("toggle.debug-shortcuts", "Toggles debug shortcuts for a cleaner gameplay experience")][UnityEngine.Scripting.Preserve]
        static void DisableDebugShortcuts() {
            CheatController.CheatShortcutsEnabled = !CheatController.CheatShortcutsEnabled;
            
            QuantumConsole.Instance.LogToConsoleAsync("Debug shortcuts: " + (CheatController.CheatShortcutsEnabled ? "enabled" : "disabled"));
        }
        
        [Command("scene.change", "Changes the scene to the specified one", allowWhiteSpaces:true)][UnityEngine.Scripting.Preserve]
        static void ChangeScene([SceneName] string sceneName) {
            if (Hero.Current == null) {
                ScenePreloader.StartNewGame(SceneReference.ByName(sceneName));
                return;
            }
            
            Portal.MapChangeTo(Hero.Current, SceneReference.ByName(sceneName), World.Services.Get<SceneService>().ActiveSceneRef, null);
        }
        
        [Command("hero.level-up", "Levels up the hero by the specified amount")][UnityEngine.Scripting.Preserve]
        static void LevelUpHero(int levels) {
            HeroDevelopment currentDevelopment = Hero.Current?.Development;
            if (currentDevelopment == null) {
                QuantumConsole.Instance.LogToConsoleAsync("No hero found");
                return;
            }
            currentDevelopment.LevelUpTo(currentDevelopment.Level.BaseInt + levels);
        }

        [Command("hero.set-arthur-memory-reminder", "Reminds the hero to go to the bonfire and talk with Arthur")][UnityEngine.Scripting.Preserve]
        static void SetArthurMemoryReminder() {
            Hero.Current.Stat(HeroStatType.WyrdWhispers).IncreaseBy(1);
        }

        static int s_cycleSsTeleports = -1;
        [Command("ss.teleport", "Teleports hero to defined or cyclical screenshot position")][UnityEngine.Scripting.Preserve]
        static async void ScreenshotTeleport([PortalName(PortalType.ScreenshotPosition)] string teleportName = "") {
            if (Hero.Current == null) return;
            
            var sceneReference = World.Services.Get<SceneService>().ActiveSceneRef;
            var targetPortal = GetTargetPortal(teleportName, sceneReference, PortalType.ScreenshotPosition);

            if (targetPortal == null) {
                QuantumConsole.Instance.LogToConsoleAsync("The provided portal cannot be found in the current domain");
                return;
            }
            
            await Portal.FastTravel.To(Hero.Current, targetPortal, false);
            Hero.Current.Trigger(Hero.Events.HideWeapons, true);
            Hero.Current.TrySetMovementType<NoClipMovement>();
        }

        [Command("ss.capture", "Takes a screenshot without ui")][UnityEngine.Scripting.Preserve]
        static async void TakeScreenshot(int supersize = 1) {
            var marvinMode = MarvinMode.GetOrCreateMarvin();
            
            QuantumConsole.Instance.Deactivate();
            marvinMode.HideUI();
            
            Directory.CreateDirectory(Application.persistentDataPath + "/Screenshots");
            ScreenCapture.CaptureScreenshot(Application.persistentDataPath + "/Screenshots/Screenshot_" + DateTime.UtcNow.ToString("yyyyMMdd_hhmmss") + ".png", supersize);
            await Awaitable.NextFrameAsync(); // Wait for the screenshot to capture before reenabling ui
            
            marvinMode.ShowUI();
            QuantumConsole.Instance.Activate();
        }

        static Portal GetTargetPortal(string teleportName, SceneReference sceneReference, PortalType type) {
            var portalsInOrder = World.All<Portal>()
                                      .Where(p => p.CurrentDomain == sceneReference.Domain && p.Type == type)
                                      .ToArray();
            if (teleportName.IsNullOrWhitespace()) {
                s_cycleSsTeleports = ++s_cycleSsTeleports % portalsInOrder.Length;
                return portalsInOrder[s_cycleSsTeleports];
            }
            return portalsInOrder.FirstOrDefault(p => p.LocationDebugName == teleportName);
        }

        #region Weather
        static GameRealTime s_gameRealTime;
        static GameRealTime GameRealTime => s_gameRealTime ??= World.Any<GameRealTime>();
        static WeatherController WeatherController => GameRealTime.Element<WeatherController>();
        
        [Command("weather.next-preset", "Begins rain")][UnityEngine.Scripting.Preserve]
        static void BeginRain() {
            WeatherController.ManualOverrideToNextPreset();
        }
        
        [Command("weather.toggle", "Toggles rain/snow")][UnityEngine.Scripting.Preserve]
        static void ToggleDisablingWeather() {
            if (WeatherController.ManuallyPrecipitationDisabled) {
                WeatherController.ResumePrecipitation(true);
            } else {
                WeatherController.StopPrecipitation(true);
            }

            QuantumConsole.Instance.LogToConsoleAsync("Precipitation: " + (WeatherController.ManuallyPrecipitationDisabled ? "disabled" : "enabled"));
        }
        
        static TimeBlocker s_timeBlocker;
        public static bool DaytimeEnabled => s_timeBlocker != null;
        
        [Command("weather.toggle-cycle", "Toggles day/night cycle")][UnityEngine.Scripting.Preserve]
        static void ToggleWeatherTime() {
            if (DaytimeEnabled) {
                s_timeBlocker.Discard();
            } else {
                s_timeBlocker = World.Add(new TimeBlocker(nameof(QCDebugTools), TimeType.Weather));
            }

            QuantumConsole.Instance.LogToConsoleAsync("Daytime cycle: " + (DaytimeEnabled ? "disabled" : "enabled"));
        }
        
        #endregion
        
        [Command("location.spec-name", "Prints the spec name of the location you look at. It also copy it to cliboard")][UnityEngine.Scripting.Preserve]
        static void PrintLocationDisplayName() {
            var hero = Hero.Current;
            if (!hero) {
                return;
            }
            var raycaster = hero.VHeroController.Raycaster;
            if (!raycaster) {
                return;
            }
            if (!raycaster.NPCRef.TryGet(out var location)) {
                return;
            }
            var spec = location.Spec;
            var scene = spec.gameObject.scene;
            var name = scene.IsValid() ? $"{scene.name}/{spec.gameObject.PathInSceneHierarchy(true)}" : spec.name;
            QuantumConsole.Instance.LogToConsole(name);
            GUIUtility.systemCopyBuffer = name;
        }

        [Command("hero.print-colliders", "Prints all colliders in front of the hero")][UnityEngine.Scripting.Preserve]
        static void PrintAllCollidersInFrontOfHero() {
            Hero hero = Hero.Current;
            RaycastCheck raycastCheck = new() {
                accept = ~RenderLayers.Mask.Player
            };
            
            hero.VHeroController.Raycaster.GetViewRay(out var origin, out var direction);
            raycastCheck.CheckMultiHit(origin, direction, out List<HitResult> hitResults, 20, 1);
            
            foreach (HitResult hitResult in hitResults.OrderBy(r => r.Point.DistanceTo(origin))) {
                string colliderType = hitResult.Collider.isTrigger 
                    ? "trigger : '" 
                    : "collider: '";
                GameObject colliderGameObject = hitResult.Collider.gameObject;
                string hitInfo = colliderType + hitResult.Point.DistanceTo(origin) + "' > " + colliderGameObject.scene.name + "/" + colliderGameObject.PathInSceneHierarchy(true);
                
                QuantumConsole.Instance.LogToConsole(hitInfo);
                Log.Important?.Warning(hitInfo, hitResult.Collider, LogOption.NoStacktrace);
            }
        }

        [Command("toggle.debug-names", "Toggles debug names for locations")][UnityEngine.Scripting.Preserve]
        static void ToggleDebugNames() {
            var currentVal = DebugProjectNames.Basic;
            DebugProjectNames.SetActiveBasic(!currentVal);
            
            QuantumConsole.Instance.LogToConsoleAsync("Debug names: " + (!currentVal ? "enabled" : "disabled"));
        }
        
        [Command("toggle.debug-names-extended", "Toggles extended debug names for locations")][UnityEngine.Scripting.Preserve]
        public static void ToggleExtendedDebugNames() {
            var currentVal = DebugProjectNames.Extended;
            DebugProjectNames.SetActiveExtended(!currentVal);
            
            QuantumConsole.Instance.LogToConsoleAsync("Extended debug names: " + (!currentVal ? "enabled" : "disabled"));
        }
        
        [Command("toggle.map-fog-of-war", "Toggles fog of war hiding unexplored parts of the map")][UnityEngine.Scripting.Preserve]
        public static void ToggleFogOfWar(bool? setTo = null) {
            var newValue = MapUI.ToggleFogOfWar(setTo);
            QuantumConsole.Instance.LogToConsoleAsync("Extended debug names: " + (newValue ? "enabled" : "disabled"));
        }

        [Command("toggle.debug-ai-perception", "Toggles debug vision cones for AI and noise markers")][UnityEngine.Scripting.Preserve]
        static void ToggleDebugPerception(bool? setTo = null, bool vision = true, bool noise = true, bool target = true) {
            bool currentValue = Perception.DebugMode;
            if ((setTo ?? false) && currentValue) {
                ToggleDebugPerception(false);
                DelayToggleDebugPerception(true, vision, noise, target).Forget();
                return;
            }
            Perception.debugMode = !currentValue;
            SafeEditorPrefs.SetInt("debug.perception", currentValue ? 0 : 1);
            
            Perception.debugVisionMode = vision;
            Perception.debugNoiseMode = noise;
            Perception.debugTargetMode = target;

            QuantumConsole.Instance.LogToConsoleAsync("Debug Perception: " + (!currentValue ? "enabled" : "disabled"));
        }
        
        static async UniTaskVoid DelayToggleDebugPerception(bool? setTo, bool vision, bool noise, bool target ) {
            await UniTask.DelayFrame(2);
            ToggleDebugPerception(setTo, vision, noise, target);
        }

        [Command("test.wyrd-stalker", "")][UnityEngine.Scripting.Preserve]
        static void TestWyrdStalker(bool forceActive = false) {
            if (forceActive) {
                Hero.Current.Development.WyrdSoulFragments.Unlock(WyrdSoulFragmentType.Prologue);
                Hero.Current.Development.WyrdSoulFragments.Unlock(WyrdSoulFragmentType.Excalibur);
            }
            bool success = World.Any<HeroWyrdStalker>()?.TrySpawn(true) ?? false;
            QuantumConsole.Instance.LogToConsoleAsync("WyrdStalker spawned " + ( success ? "successfully" : "unsuccessfully"));
        }

        [Command("hud.show-all-notification-presenters", "")][UnityEngine.Scripting.Preserve]
        static void ShowAllNotificationPresenters() {
            foreach (IAdvancedNotificationBufferPresenter advancedNotificationBuffer in World.All<IAdvancedNotificationBufferPresenter>()) {
                advancedNotificationBuffer.ForceDisplayingNotifications();
            }
        }
        
        [Command("audio.print-audio-core-state", "Prints the current audio core state (what audio is playing currently etc.). It also saves it to the clipboard")][UnityEngine.Scripting.Preserve]
        static void PrintAudioCoreStateDisplayName() {
            AudioCore audioCore = World.Services.TryGet<AudioCore>();
            if (audioCore == null) {
                return;
            }
            var audioCoreState = audioCore.GetAudioCoreState();
            QuantumConsole.Instance.LogToConsole(audioCoreState);
            GUIUtility.systemCopyBuffer = audioCoreState;
        }
        
        [Command("audio.print-global-parameters", "Prints global parameters state.")][UnityEngine.Scripting.Preserve]
        static void PrintAudioGlobalParameters() {
            AudioCore audioCore = World.Services.TryGet<AudioCore>();
            if (audioCore == null) {
                return;
            }
            QuantumConsole.Instance.LogToConsole(AudioCore.GetGlobalParametersState());
        }
        
        [Command("audio.recalculate-priority", "Recalculates all audio-core emitters priorities to determine correct music to play")][UnityEngine.Scripting.Preserve]
        static void RecalculateAudioCorePriority() {
            AudioCore audioCore = World.Services.TryGet<AudioCore>();
            if (audioCore == null) {
                return;
            }
            audioCore.ForceRecalculateSoundPriority();
        }
        
        [Command("show-cloud-conflict-ui", "")][UnityEngine.Scripting.Preserve]
        static void ShowCloudConflictUI() {
            QuantumConsole.Instance.Deactivate();
            GameObject conflictResolutionPrefab = Resources.Load<GameObject>("Prefabs/CloudConflictResolution");
            var go = Object.Instantiate(conflictResolutionPrefab, World.Services.Get<CanvasService>().MainTransform);
            CloudConflictUI conflictUI = go.GetComponent<CloudConflictUI>();
            conflictUI.Init(DateTime.Now, DateTime.Now, () => Object.Destroy(go), () => Object.Destroy(go));
        }
        
        [Command("run-character-creator-debug", "Opens the character creator and load GameTestArena")][UnityEngine.Scripting.Preserve]
        static void ShowCharacterCreator() {
            QuantumConsole.Instance.Deactivate();
            SceneSets jailTutorial = CommonReferences.Get.presetSelectorConfig.JailTutorial;
            
            StartGameData data = new() {
                withHeroCreation = true,
                withTransitionToCamera = true,
                sceneReference = SceneReference.ByName("GameplayTestArena"),
                characterPresetData = jailTutorial.presets.FirstOrDefault(),
            };
            
            TitleScreenUtils.StartNewGame(data);
        }
        
        [Command("hero.unlock-all-skills", "Unlocks all skills")][UnityEngine.Scripting.Preserve]
        static void UnlockAllSkills() {
            HeroTalents talents = Hero.Current?.Talents;
            if (talents == null) {
                QuantumConsole.Instance.LogToConsoleAsync("No hero found");
                return;
            }
            
            Hero.Current.Development.WyrdSoulFragments.Unlock(WyrdSoulFragmentType.Prologue);
            Hero.Current.Development.WyrdSoulFragments.Unlock(WyrdSoulFragmentType.Excalibur);
            
            foreach (var talentTable in talents.Elements<TalentTable>()) {
                var currency = Hero.Current.Stat(talentTable.TreeTemplate.CurrencyStatType);
                foreach (var talent in talentTable.Elements<Talent>()) {
                    while (talent.EstimatedLevel < talent.MaxLevel) {
                        currency.SetAtLeastTo(1);
                        if (!talent.AcquireNextTemporaryLevel()) {
                            break;
                        }
                    }
                    talent.ApplyTemporaryLevels();
                }
            }
            Hero.Current.RestoreStats();
        }

#if UNITY_EDITOR
        [Command("start-all-quests", "Starts all quest templates regardless of validity")][UnityEngine.Scripting.Preserve]
        static void StartAllQuests() {
            var allQuest = TemplatesProvider.EditorGetAllOfType<QuestTemplate>();

            foreach (QuestTemplate quest in allQuest) {
                if (string.IsNullOrWhiteSpace(quest.DisplayName)) {
                    continue;
                }
                
                EDITOR_QuestTemplateDebug.EDITOR_StartQuest(quest);
                EDITOR_QuestTemplateDebug.EDITOR_StartObjective(quest);
            }
        }
#endif
    }
}