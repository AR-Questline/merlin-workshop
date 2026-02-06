using System;
using System.Collections.Generic;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes;
using Awaken.TG.Main.Utility.Patchers.Dlc;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.Utility.Patchers {
    public class PatcherService : IService {
        // === State
        string OriginalVersion { get; set; }
        Version MostRecentlyLoadedVersion { get; set; }

        Patcher[] _patchers = {
            // new Patcher019_020(),
            // new Patcher021_022(),
            // new Patcher022_023(),
            // new Patcher024_025(),
            // new Patcher026_027(),
            // new Patcher028_029(),
            // new Patcher036_037(),
            // new Patcher040_041(),
            // new Patcher041_042(),
            new Patcher051_052(),
            new Patcher054_055(),
            new Patcher066_067(),
            new Patcher100_101(),
            new Patcher101_102(),
            new Patcher102_103(),
            new Patcher104_105(),
            new Patcher106_106(),
            new Patcher108_110(),
            new Patcher110_112(),
            new Patcher112_113(),
            new Patcher113_114(),
            new Patcher114_115(),
            new Patcher115_115a(),
            new Patcher115_115b(),
            new Patcher115_115c(),
            new Patcher115_115e(),
            new Patcher_Final(), // put final as last
        };

        DlcPatcher[] _dlcPatchers = {
            new DlcPatcherContentPack(),
        };

        public static string CurrentVersionStr => Application.version;
        public static Version CurrentVersion => new Version(CurrentVersionStr);
        
        // === Constructor
        public PatcherService(GameConstants constants) {
            RunGamePatch(constants);
        }

        // === Patching methods
        void RunGamePatch(GameConstants constants) {
            OriginalVersion = PrefMemory.GetString("Version", CurrentVersionStr);
            var version = new Version(OriginalVersion);
            
            var lastWipeSavesVersion = new Version(constants.wipeSavesOnVersion);
            if (version < lastWipeSavesVersion || Configuration.GetBoolExact("wipe_saves")) {
                Patcher_DeleteSaves.WipeSaves();
            }
            
            foreach (Patcher patcher in IteratePatchers(version)) {
                patcher.StartGamePatch();
                version = patcher.PatcherFinalVersion;
            }
            PrefMemory.Set("Version", version.ToString(), true);
            
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, OnSceneInitializationEnded);
        }

        void OnSceneInitializationEnded(LoadingScreenUI l) {
            TryAfterGameLoadedPatch(l);
            TryAfterGameLoadedPatchDlc(l);
        }

        void TryAfterGameLoadedPatch(LoadingScreenUI l) {
            if (l.LoadingType == LoadingType.Full || l.LoadingOperation is NewGamePlusLoading {NewGameFromTitleScreen: true}) {
                foreach (Patcher patcher in IteratePatchers(MostRecentlyLoadedVersion)) {
                    patcher.AfterGameLoadedPatch();
                }
            }
        }

        void TryAfterGameLoadedPatchDlc(LoadingScreenUI l) {
            if (PlatformUtils.IsEditor && (l.LoadingType is LoadingType.Map && l.PreviousScene == null) // Handle starting game from specific scene in editor
                || l.LoadingType is LoadingType.Full or LoadingType.NewGame) {
                foreach (var dlcPatcher in IterateDlcPatchers(HeroDlcHandler.PreviousCategoriesAtInitialize)) {
                    dlcPatcher.AfterGameLoadedPatch();
                }
            }
        }

        public void BeforeDeserializedModel(Version version, Model model) {
            foreach (var patcher in IteratePatchers(version)) {
                patcher.BeforeDeserializedModel(model);
            }
        }
        
        public bool AfterDeserializedModel(Version version, Model model) {
            foreach (var patcher in IteratePatchers(version)) {
                if (patcher.AfterDeserializedModel(model) == false) {
                    Log.Important?.Error($"{patcher.GetType().Name} removed model {model.ID} imported from version {version}");
                    return false;
                }
            }
            return true;
        }
        
        public void AfterDeserializedService(Version version, SerializedService service) {
            foreach (var patcher in IteratePatchers(version)) {
                patcher.AfterDeserializedService(service);
            }
        }

        public void AfterRestorePatch(Version version) {
            MostRecentlyLoadedVersion = version;
            foreach (var patcher in IteratePatchers(version)) {
                patcher.AfterRestorePatch();
            }
        }

        public async UniTask CheckAllSaveSlots(IProgress<float> progress) {
            foreach (var patcher in IteratePatchers(new Version(OriginalVersion))) {
                await patcher.CheckAllSaveSlots(progress);
            }
        }
        
        public void RunSpecPatches(Scene[] scenes) {
            Services services = World.Services;
            
            var sceneService = services.TryGet<SceneService>();
            var memory = services.TryGet<GameplayMemory>();

            if (sceneService == null || memory == null) {
                return;
            }
            
            // HoS scene fixes
            if (sceneService.ActiveSceneRef.Name == "CampaignMap_HOS") {
                TryFixHuntForWyrddeerQuest(memory);
            }
        }

        static void TryFixHuntForWyrddeerQuest(GameplayMemory memory) {
            // check TASK_HuntForWyrddeer state
            ContextualFacts wyrdDeerTaskFacts = memory.Context("5312476f24c5950418d5a861efc917c8");
            if (wyrdDeerTaskFacts.Get("state", QuestState.NotTaken) == QuestState.Active) {
                // Scene id of quest summer special deer spawner
                memory.Context(Location.DiscardedPlacesKey).Remove("CM_KingsRoad_Bridge_296742655_963526087843284630_1");
            }
        }

        // === Helpers
        IEnumerable<Patcher> IteratePatchers(Version version) {
            foreach (Patcher patcher in _patchers) {
                if (patcher.CanPatch(version)) {
                    yield return patcher;
                    version = patcher.PatcherFinalVersion;
                }
            }
        }

        IEnumerable<DlcPatcher> IterateDlcPatchers(DlcCategoryFlags previouslyActiveDlcCategories) {
            foreach (DlcPatcher patcher in _dlcPatchers) {
                if (patcher.CanPatch(previouslyActiveDlcCategories)) {
                    yield return patcher;
                }
            }
        }
    }
}
