using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Saving.SaveSlots;

namespace Awaken.TG.Main.UI.TitleScreen {
    public class StartGameData {
        public bool withHeroCreation = false;
        public bool withTransitionToCamera = false;
        public Action onStart;
        public SceneReference sceneReference;
        public CharacterBuildPreset characterPresetData;
    }
}