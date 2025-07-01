using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit {
    [Searchable]
    public class PresenterDataProvider : ScriptableObject, IService {
        public const float DebugDurationFastFade = 0.4f;
        public const float DebugDurationFastMove = 0.1f;
        public const float DebugDurationFastHeight = 0.1f;
        public const float DebugDurationFastVisibility = 0.5f;
        
        [FoldoutGroup("ArenaSpawner")] public PArenaSpawnerData arenaSpawnerData;
        [FoldoutGroup("Container")] public PContainerElementData containerElementData;
        [FoldoutGroup("Notifications")] public PExpNotificationData expNotificationData;
        [FoldoutGroup("Notifications")] public PItemNotificationData itemNotificationData;
        [FoldoutGroup("Notifications")] public PLocationNotificationData locationNotificationData;
        [FoldoutGroup("Notifications")] public PSpecialItemNotificationData specialItemNotificationData;
        [FoldoutGroup("Notifications")] public PProficiencyNotificationData proficiencyNotificationData;
        [FoldoutGroup("Notifications")] public PLevelUpNotificationData levelUpNotificationData;
        [FoldoutGroup("Notifications")] public PQuestNotificationData questNotificationData;
        [FoldoutGroup("Notifications")] public PObjectiveNotificationData objectiveNotificationData;
        [FoldoutGroup("Notifications")] public PRecipeNotificationData recipeNotificationData;
        [FoldoutGroup("Notifications")] public PJournalUnlockNotificationData journalUnlockNotificationData;
        [FoldoutGroup("Notifications")] public PWyrdInfoNotificationData wyrdInfoNotificationData;
    }
}