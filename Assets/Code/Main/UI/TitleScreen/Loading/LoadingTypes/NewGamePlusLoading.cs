using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes {
    /// <summary>
    /// Start a new game from gameplay, not main menu.
    /// </summary>
    public class NewGamePlusLoading : NewGameLoading {
        readonly int _newGamePlusLevel;
        readonly SaveSlot _saveSlotToLoad;
        
        byte[] _newGamePlusData;
        ContextualFacts[] _newGamePlusMemories;

        public bool NewGameFromTitleScreen => _saveSlotToLoad != null;
        
        public NewGamePlusLoading(SceneReference sceneReference, [CanBeNull] SaveSlot saveSlotToLoad, int newGamePlusLevel) : base(sceneReference) {
            _newGamePlusLevel = newGamePlusLevel;
            _saveSlotToLoad = saveSlotToLoad;
        }
        
        public override IEnumerable<SceneReference> ScenesToUnload(SceneReference previousScene) {
            SceneService sceneService = World.Services.Get<SceneService>();
            yield return sceneService.AdditiveSceneRef;
            yield return sceneService.MainSceneRef;
        }

        public override IEnumerator BeforeDroppingPreviousDomains() {
            // Wait for all templates loaded
            var templatesProvider = World.Services.Get<TemplatesProvider>();
            if (!templatesProvider.AllLoaded) {
                Log.Marking?.Warning("Waiting for templates to load");
                while (!templatesProvider.AllLoaded) {
                    yield return null;
                }
                Log.Marking?.Warning("All templates loaded");
            }
            
            yield return new WaitForEndOfFrame();
            if (_saveSlotToLoad != null) {
                LoadSave.Get.LoadOnlyGameplayToCache(_saveSlotToLoad);
                GameplayConstructor.RestoreGameplay(_saveSlotToLoad, true);
                for (int i = 0; i < 5; i++) {
                    yield return null;
                }
            }
            NewGamePlusUtils.SaveCurrentDomain(_newGamePlusLevel, out _newGamePlusData, out _newGamePlusMemories);
            for (int i = 0; i < 3; i++) {
                yield return null;
            }
            yield return new WaitForEndOfFrame();
        }

        public override void OnComplete(IMapScene _) {
            // Construct gameplay from scratch
            GameplayConstructor.CreateNewGamePlusGameplay(_newGamePlusLevel, _newGamePlusData, _newGamePlusMemories);
            Hero.Current.RestoreStats();
            Hero.Current.ListenToLimited(Hero.Events.OnWeaponBeginEquip, () => HideWeaponsNextFrame().Forget(), Hero.Current);
        }

        async UniTaskVoid HideWeaponsNextFrame() {
            if (!await AsyncUtil.DelayFrame(Hero.Current)) {
                return;
            }
            Hero.Current.Trigger(Hero.Events.HideWeapons, false);
        }
    }
}