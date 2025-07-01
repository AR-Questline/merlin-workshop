using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests {
    public class AutoAchievementsService : MonoBehaviour, IService {
        [TemplateType(typeof(AchievementTemplate))]
        public TemplateReference[] achievementReferences = new TemplateReference[0];

        public void SpawnMissing() {
            var gameplayMemory = World.Services.Get<GameplayMemory>();
            foreach (TemplateReference achievementReference in achievementReferences) {
                var achievementTemplate = achievementReference.Get<AchievementTemplate>();
                bool alreadyTaken = QuestUtils.AlreadyTaken(gameplayMemory, achievementReference);
                if (!alreadyTaken) {
                    Quest quest = new Quest(achievementTemplate);
                    World.Add(quest);
                }
            }
        }
    }
}