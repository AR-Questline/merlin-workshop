using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public abstract class BaseSceneSpecificTrackerAttachment : BaseTrackerAttachment {
        [FoldoutGroup("Setup"), InfoBox("Is visible when on incorrect scene. " +
                                        "\n{sceneName} is used for target scene name.")]
        [LocStringCategory(Category.QuestTracker)]
        public LocString changeSceneDescription;
        [FoldoutGroup("Setup")]
        public SceneReference targetScene;
    }
}