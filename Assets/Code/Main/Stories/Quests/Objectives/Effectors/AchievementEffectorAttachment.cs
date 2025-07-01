using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Effectors, "Used to set an achievement when objective changes state.")]
    public class AchievementEffectorAttachment : MonoBehaviour, IAttachmentSpec {
        public string achievementId;
        public int sonyId;
        public int microsoftId;
        public AchievementEffector.AggregationType aggregationType = AchievementEffector.AggregationType.Max;
        
        public Element SpawnElement() {
            return new AchievementEffector(achievementId);
        }

        public bool IsMine(Element element) {
            return element is AchievementEffector effector && effector.BaseAchievementID == achievementId;
        }
    }
}