using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Storage;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.NewGamePlus {
    public static class NewGamePlusUtils {
        const string EndGameQuestGUID = "c0b2861bb35653b41b0dfe3507352d56";
        const string HorseArmorItemGUID = "317352ec19ebbf04f83a6ab83a806513";

        static readonly Type[] ServiceTypesToSave = {
            typeof(IdStorage.SaveIdStorage),
        };
        static readonly string[] MemoryContextsToSave = {
            "tutorial_context"
        };
        static readonly string[] GameFinishedTags = {
            "HolyGrail:PaleLady",
            "HolyGrail:Resurect",
            "HolyGrail:Destroy",
            "HolyGrail:Myself",
            "HolyGrail:Other"
        };
        static readonly string[] ItemGUIDsToRemove = {
            "c0939462321564d45b21dbacd59a55d7", // ItemTemplate_Weapon_2H_Sword_Heavy_Tier5_SwordOfGiants
            "fe97c1e1609a2a34ba2645669689728c" // ItemTemplate_Weapon_1H_Sword_Heavy_Tier5_SwordOfGiants_OneHanded
        };

        public static bool CanBeTriggeredRightNow => LoadSave.Get.CanSystemSave();
        public static bool IsAvailable => World.Any<Quest>(q => q.State == QuestState.Completed && q.Template.GUID.Equals(EndGameQuestGUID)) || GameFinishedTags.Any(StoryFlags.Get);
        
        // === Helpers
        public static bool AnyNewGamePlusEligibleSave() {
            foreach (var saveSlot in World.All<SaveSlot>()) {
                if (saveSlot.AllowNewGamePlus) {
                    return true;
                }
            }
            return false;
        } 

        // === Start NG+
        public static void LoadSaveAndStartNewGamePlus(SaveSlot slot) {
            LoadNewGamePlus(slot);
        }

        public static void StartNewGamePlusDuringGameplay() {
            if (!CanBeTriggeredRightNow) {
                Log.Critical?.Error("Cannot start New Game Plus: Save system doesn't allow for saves. Game can be already being saved (2 saves in the same moment are not supported)");
                return;
            }
            LoadNewGamePlus(null);
        }

        static void LoadNewGamePlus([CanBeNull] SaveSlot saveSlotToLoad) {
            SceneSets jailTutorial = CommonReferences.Get.presetSelectorConfig.JailTutorial;
            
            StartGameData data = new() {
                withHeroCreation = false,
                sceneReference = jailTutorial.Scene,
                characterPresetData = jailTutorial.presets.FirstOrDefault()
            };
            
            int ngLevel = saveSlotToLoad != null
                ? saveSlotToLoad.NewGamePlusLevel + 1
                : NewGamePlusSystem.Level + 1;
            TitleScreenUtils.StartNewGamePlus(data, saveSlotToLoad, ngLevel);
        }
        
        // === Save Data from Current Domain
        public static void SaveCurrentDomain(int ngPlusLevel, out byte[] newGamePlusData, out ContextualFacts[] newGamePlusMemories) {
            PrepareHero(ngPlusLevel);
            SaveMemory(out newGamePlusMemories);
            StartSavingGameplayModels(out newGamePlusData);
        }

        static void PrepareHero(int ngPlusLevel) {
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            Hero.Current.HeroStats.WyrdWhispers.SetTo(0);
            Hero.Current.Development.WyrdSoulFragments.LockAll();
            Hero.Current.HeroStats.WyrdMemoryShards.SetTo(0);

            foreach (var table in Hero.Current.Talents.Elements<TalentTable>()) {
                if (!string.IsNullOrEmpty(table.TreeTemplate.RequiredFlag)) {
                    table.Reset();
                }
            }
            
            
            foreach (var item in Hero.Current.HeroItems.Items.ToList()) {
                if (!ShouldItemBeSaved(item)) {
                    Hero.Current.HeroItems.Remove(item, true, false);
                }
            }

            Hero.Current.Storage.RequestItems();
            foreach (var item in Hero.Current.Storage.Items.ToList()) {
                if (!ShouldItemBeSaved(item)) {
                    Hero.Current.Storage.Remove(item, true);
                }
            }
            Hero.Current.Storage.ReleaseItems();
            
            Hero.Current.Element<QuestTracker>().Track(null);
            Hero.Current.Element<HeroRecipes>().knownRecipes.Clear();
            
            var statusesToRemove = CommonReferences.Get.StatusesToRemoveOnNgPlus;
            var heroStatuses = Hero.Current.Statuses;
            var ngPlusStatusTemplate = CommonReferences.Get.NewGamePlusStatus;
            Status ngPlusStatus = null;
            foreach (var status in heroStatuses.AllStatuses.ToArraySlow()) {
                foreach (var templateToRemove in statusesToRemove) {
                    if (status.Template.GUID.Equals(templateToRemove.GUID)) {
                        status.Discard();
                    } else if (status.Template.Equals(ngPlusStatusTemplate)) {
                        ngPlusStatus = status;
                    }
                }
            }

            ngPlusStatus ??= heroStatuses.AddStatus(ngPlusStatusTemplate, StatusSourceInfo.FromStatus(ngPlusStatusTemplate).WithCharacter(Hero.Current)).newStatus;
            ngPlusStatus.SetStacksTo(ngPlusLevel - 1);

            AdvancedNotificationBuffer.AllNotificationsSuspended = false;
        }

        static void SaveMemory(out ContextualFacts[] newGamePlusMemories) {
            newGamePlusMemories = new ContextualFacts[MemoryContextsToSave.Length];
            var memory = World.Services.Get<GameplayMemory>();
            for (int i = 0; i < MemoryContextsToSave.Length; i++) {
                newGamePlusMemories[i] = memory.Context(MemoryContextsToSave[i]);
            }
        }

        static void StartSavingGameplayModels(out byte[] newGamePlusData) {
            var allInOrder = World.AllInOrderReadonlyNotValidated();
            var allCount = allInOrder.Count;
            var toSaveIndices = new UnsafeBitmask((uint)allCount, Allocator.Temp);
            for (var i = 0u; i < allCount; i++) {
                var model = allInOrder.BackingArray[i];
                if (model.CurrentDomain == Domain.Gameplay && model.HasBeenDiscarded == false && ShouldModelBeSaved(model)) {
                    toSaveIndices.Up(i);
                    model.PrepareForSaving();
                }
            }

            using (var stream = new NativeMemoryStream(8192, Allocator.Temp)) {
                LoadSave.Get.SaveSystem.SerializeNewGamePlusCache(stream, allInOrder, toSaveIndices, ServiceTypesToSave);
                newGamePlusData = stream.ToArray();
            }

            toSaveIndices.Dispose();
        }

        static bool ShouldModelBeSaved(IModel model) {
            while (true) {
                switch (model) {
                    case IElement element:
                        model = element.GenericParentModel;
                        continue;
                    case Hero hero:
                        return true;
                    case Item item:
                        return ShouldItemBeSaved(item);
                    case Quest quest:
                        return quest.QuestType is QuestType.Achievement;
                    case PlayerJournal:
                        return true;
                    case GameRealTime:
                        return true;
                }
                return false;
            }
        }

        static bool ShouldItemBeSaved(Item item) {
            if (item is not { IsQuestItem: false, IsKey: false, IsReadable: false } || item.Quality == ItemQuality.Quest) {
                if (!item.Template.GUID.Equals(HorseArmorItemGUID)) {
                    return false;
                }
            }
            foreach (var guid in ItemGUIDsToRemove) {
                if (item.Template.GUID.Equals(guid)) {
                    return false;
                }
            }
            
            if (item.Owner is not Hero and not HeroStorage) {
                return false;
            }
            return true;
        }
        
        // === Load NG+
        public static void RestoreBeforeGameplayInit(ContextualFacts[] newGamePlusMemories) {
            LoadMemory(newGamePlusMemories);
        }
        
        public static void RestoreOnHeroCreation(byte[] newGamePlusData) {
            LoadGameplayData(newGamePlusData);
        }
        
        static void LoadMemory(ContextualFacts[] newGamePlusMemories) {
            var memory = World.Services.Get<GameplayMemory>();
            for (int i = 0; i < MemoryContextsToSave.Length; i++) {
                memory.Context(MemoryContextsToSave[i]).CopyFrom(newGamePlusMemories[i]);
            }
        }

        static void LoadGameplayData(byte[] newGamePlusData) {
            using (var stream = new MemoryStream(newGamePlusData)) {
                LoadSave.Get.LoadSystem.DeserializeNewGamePlusCache(stream, Domain.Gameplay);
            }
        }
        
        // === Check Old Saves

        public static async UniTask FindAndFixSavesNotMarkedAsNgReady(IProgress<float> progress) {
            const float prepareProgress = 0.1f;
            const float searchProgress = 0.9f;
            const float saveProgress = 1.0f;
            Log.Marking?.Warning("[Fixing Missing NG+ Saves] Save Search Prepare");
            
            float progressValue = 0;
            progress?.Report(progressValue);
            
            var templatesProvider = World.Services.Get<TemplatesProvider>();
            templatesProvider.StartLoading();
            if (!templatesProvider.AllLoaded) {
                Log.Marking?.Warning("Waiting for templates to load");
                while (!templatesProvider.AllLoaded) {
                    await UniTask.Yield();
                    if (progressValue <= prepareProgress) {
                        progressValue += 0.001f;
                        progress?.Report(progressValue);
                    }
                }
                Log.Marking?.Warning("All templates loaded");
            }
            
            progressValue = prepareProgress;
            progress?.Report(prepareProgress);
            
            Log.Marking?.Warning("[Fixing Missing NG+ Saves] Save Search Start");
            var saveSlotsToMarkAsNgReady = new List<SaveSlot>();
            var allSaveSlots = World.All<SaveSlot>();
            uint count = allSaveSlots.Count();
            float oneSaveSlotProgress = (searchProgress - prepareProgress) * (1f / count);
           
            Log.disableLogs = true;
            foreach (var saveSlot in allSaveSlots) {
                if (!IsNgReadyCandidate(saveSlot)) {
                    continue;
                }
                if (!saveSlot.AllowNewGamePlus && CheckIfSaveIsNgReady(saveSlot)) {
                    saveSlotsToMarkAsNgReady.Add(saveSlot);
                }
                await UniTask.Yield();
                CleanUpDomain();
                await UniTask.Yield();
                progressValue += oneSaveSlotProgress;
                progress?.Report(progressValue);
            }
            Log.disableLogs = false;

            progressValue = searchProgress;
            progress?.Report(searchProgress);

            Log.Marking?.Warning($"[Fixing Missing NG+ Saves] Save Search Ended - found {saveSlotsToMarkAsNgReady.Count} saves from {count}.");
            oneSaveSlotProgress = (saveProgress - searchProgress) * (1f / saveSlotsToMarkAsNgReady.Count());
            foreach (var saveSlot in saveSlotsToMarkAsNgReady) {
                await UpdateSaveSlotMetaData(saveSlot);
                progressValue += oneSaveSlotProgress;
                progress?.Report(progressValue);
            }
            
            progress?.Report(saveProgress);
        }

        static bool IsNgReadyCandidate(SaveSlot saveSlot) {
            if (saveSlot.PlayRealTime.TotalHours < 10) {
                return false;
            }
            if (saveSlot.HeroLevel < 30) {
                return false;
            }
            return true;
        }

        static bool CheckIfSaveIsNgReady(SaveSlot saveSlot) {
            QuestState lastQuestState = QuestState.NotTaken;
            bool endGameTagFound = false;
            try {
                ReadGameplayDomain(saveSlot,
                    m => lastQuestState = lastQuestState != QuestState.NotTaken ? lastQuestState : CheckQuestModel(m),
                    s => endGameTagFound = endGameTagFound || CheckIfGameIsFinishedByTags(s));
            } catch (Exception e) {
                Debug.LogError($"[Fixing Missing NG+ Saves] Error while reading save slot {saveSlot}: {e.Message}");
                Debug.LogException(e);
                return false;
            }

            if (lastQuestState == QuestState.Active) {
                // Check if game is completed by tags. This quest can be unfinished on older version of games even though the game is finished.
                return endGameTagFound;
            }
            return lastQuestState == QuestState.Completed;
        }

        static void ReadGameplayDomain(SaveSlot saveSlot, Action<Model> modelAction, Action<SerializedService> serviceAction) {
            LoadSave.Get.LoadOnlyGameplayToCache(saveSlot);
            GameplayConstructor.RestoreGameplayJustForReading(saveSlot, modelAction, serviceAction);
        }
        
        static void CleanUpDomain() {
            World.DropDomain(Domain.Gameplay);
            World.DropDomain(Domain.SaveSlot);
            LoadSave.Get.ClearCache(Domain.Gameplay);
            LoadSave.Get.ClearCache(Domain.SaveSlot);
        }

        static QuestState CheckQuestModel(Model model) {
            if (model is not Quest quest) {
                return QuestState.NotTaken;
            }
            if (!quest.Template.GUID.Equals(EndGameQuestGUID)) {
                return QuestState.NotTaken;
            }
            return quest.State;
        }

        static bool CheckIfGameIsFinishedByTags(SerializedService service) {
            if (service is not GameplayMemory memory) {
                return false;
            }
            foreach (var tag in GameFinishedTags) {
                if (memory.Context().Get<bool>(tag)) {
                    return true;
                }
            }
            return false;
        }

        static async UniTask UpdateSaveSlotMetaData(SaveSlot saveSlot) {
            Log.Marking?.Warning($"[Fixing Missing NG+ Saves] Updating SaveSlot MetaData - {saveSlot}");
            saveSlot.MarkAsNewGamePlusReady();
            await LoadSave.Get.SaveMetadataDomainAsync(saveSlot.CurrentDomain);
            Log.Marking?.Warning($"[Fixing Missing NG+ Saves] Updated SaveSlot MetaData - {saveSlot}");
        }
        
        public static string NewGamePlusLevel(int newGameLevel, bool withAbbreviation = false) {
            if (newGameLevel <= 0) {
                return string.Empty;
            }

            var info = newGameLevel switch {
                < 4 => new string('+', newGameLevel),
                _ => $"{newGameLevel}+"
            };
             
            return withAbbreviation 
                ? $"{LocTerms.NewGamePlusAbbreviation.Translate()}{info}" 
                : info; 
        }
    }
}
