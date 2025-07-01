using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Skills;
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
            BaseInit();

            World.Add(new GlobalTime());
            World.Add(new GameRealTime());
            World.Add(new DeferredSystem());
            World.Add(new PlayerJournal());

            Hero.Create(World.Services.Get<DebugReferences>().HeroClass);

            LateInit();
        }

        public static bool RestoreGameplay(SaveSlot slot) {
            try {
                BaseInit();
                if (LoadSave.Get.LoadFromCache(Domain.Gameplay) == false) {
                    throw new Exception("Gameplay data not found");
                }
                LateInit();
                return true;
            } catch (Exception e) {
                string slotData = slot != null ? $"Slot:{slot.ID}:{slot.DisplayName}" : "Slot: null";
                Log.Critical?.Error($"Save file corrupted! Exception below. {slotData}");
                Debug.LogException(e);
                return false;
            }
        }

        static void BaseInit() {
            World.Services.Get<CommonReferences>().InitGameplay();
            World.Services.Get<FactionProvider>().EnsureInitialized();
            
            World.Services.Register(new PrefabPool()).Init();
            World.Services.Register(new GameplayMemory()).Init();
            World.Services.Register(new SceneSpecCaches());
            World.Services.Register(new NpcRegistry());
            World.Services.Register(new AutoSaving()).Init();
            World.Services.Register(new CrimeService());
            World.Services.Register(new FactionService()).Init();
            World.Services.Register(new MapService());
            World.Services.Register(new StreamedSkillGraphs());
            
            World.Add(new HUD());
            World.Add(new TutorialMaster());
        }

        static void LateInit() {
            World.Services.Get<CommonReferences>().LateInit();
            World.Services.Register(new NpcGrid()).Init();
            World.Services.Get<AutoAchievementsService>().SpawnMissing();
            World.Services.Register(new AchievementTrackingService()).Init();
        }
    }
}