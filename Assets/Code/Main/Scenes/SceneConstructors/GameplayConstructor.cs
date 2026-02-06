using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    /// <summary>
    /// Layer for single gameplay, starts when player starts/loads the game and ends when he quits to Title Screen.
    /// </summary>
    public static class GameplayConstructor {
        public static void CreateGameplay() {
            BaseInit(GameplayConstructionType.Normal);
            
            World.Add(new GlobalTime());
            World.Add(new GameRealTime());
            World.Add(new DeferredSystem());
            World.Add(new PlayerJournal());
            NewGamePlusSystem.Reset();
            Hero.Create(World.Services.Get<DebugReferences>().HeroClass);
            
            LateInit(GameplayConstructionType.Normal);
        }
        
        public static void CreateNewGamePlusGameplay(int newGamePlusLevel, byte[] newGamePlusData, ContextualFacts[] newGamePlusMemories) {
            BaseInit(GameplayConstructionType.NewGamePlusFromGameplay);
            
            NewGamePlusUtils.RestoreBeforeGameplayInit(newGamePlusMemories);
            World.Add(new GlobalTime());
            World.Add(new DeferredSystem());
            World.Add(new NewGamePlusSystem(newGamePlusLevel));
            NewGamePlusUtils.RestoreOnHeroCreation(newGamePlusData);
            
            LateInit(GameplayConstructionType.NewGamePlusFromGameplay);
        }

        public static bool RestoreGameplay(SaveSlot slot, bool beforeNewGamePlus = false) {
            try {
                var type = beforeNewGamePlus
                    ? GameplayConstructionType.BeforeNewGamePlusFromTitleScreen
                    : GameplayConstructionType.Normal;
                BaseInit(type);
                if (LoadSave.Get.LoadFromCache(Domain.Gameplay) == false) {
                    throw new Exception("Gameplay data not found");
                }
                LateInit(type);
                return true;
            } catch (Exception e) {
                string slotData = slot != null ? $"Slot:{slot.ID}:{slot.DisplayName}" : "Slot: null";
                Log.Critical?.Error($"Save file corrupted! Exception below. {slotData}");
                Debug.LogException(e);
                return false;
            }
        }

        public static bool RestoreGameplayJustForReading(SaveSlot slot, Action<Model> modelAction, Action<SerializedService> serviceAction) {
            try {
                var type = GameplayConstructionType.JustRead;
                BaseInit(type);
                if (LoadSave.Get.DomainsCache.TryGetCachedUncompressedSaveData(Domain.Gameplay, out var stream)) {
                    using (stream) {
                        LoadSave.Get.LoadSystem.DeserializeWithoutRestore(Domain.Gameplay, stream, modelAction, serviceAction);
                    }
                } else {
                    throw new Exception("Gameplay data not found");
                }
                //LateInit(type);
                return true;
            } catch (Exception e) {
                string slotData = slot != null ? $"Slot:{slot.ID}:{slot.DisplayName}" : "Slot: null";
                Log.Critical?.Error($"Save file corrupted! Exception below. {slotData}");
                Debug.LogException(e);
                return false;
            }
        }

        static void BaseInit(GameplayConstructionType type) {
            if (type != GameplayConstructionType.BeforeNewGamePlusFromTitleScreen && type != GameplayConstructionType.JustRead) {
                World.Services.Get<CommonReferences>().InitGameplay();
            }
            World.Services.Get<FactionProvider>().EnsureInitialized();

            World.Services.Register(new PrefabPool()).Init();
            World.Services.Register(new GameplayMemory()).Init();
            ActorsRegister.ClearCache();
            World.Services.Register(new SceneSpecCaches());
            World.Services.Register(new NpcRegistry());
            World.Services.Register(new UniqueNpcStash());
            World.Services.Register(new AutoSaving()).Init();
            World.Services.Register(new CrimeService());
            World.Services.Register(new FactionService()).Init();
            World.Services.Register(new MapService());
            World.Services.Register(new StreamedSkillGraphs());
            if (type != GameplayConstructionType.BeforeNewGamePlusFromTitleScreen && type != GameplayConstructionType.JustRead) {
                World.Add(new HUD());
            }

            World.Add(new TutorialMaster());
        }

        static void LateInit(GameplayConstructionType type) {
            World.Services.Get<CommonReferences>().LateInit();
            World.Services.Register(new NpcGrid()).Init();
            if (type == GameplayConstructionType.Normal) {
                World.Services.Get<AutoAchievementsService>().SpawnMissing();
            }
            World.Services.Register(new AchievementTrackingService()).Init();
        }
    }

    internal enum GameplayConstructionType : byte {
        Normal,
        BeforeNewGamePlusFromTitleScreen,
        NewGamePlusFromGameplay,
        JustRead,
    }
}