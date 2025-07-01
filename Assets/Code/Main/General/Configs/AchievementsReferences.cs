using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.General.Configs {
    [CreateAssetMenu(fileName = "AchievementsReferences", menuName = "Scriptable Objects/AchievementsReferences")]
    public class AchievementsReferences : ScriptableObject {
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference theCostOfCuriosityAchievement;
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference waaaoooaagghhAchievement;
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference preyOfTheWyrd;
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference homeSweetHome;
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference itsAlive;
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference unleashTheLegend;
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference shouldntHaveDoneThat;
        [SerializeField, TemplateType(typeof(QuestTemplateBase))] public TemplateReference ironic;
        
        [SerializeField, TemplateType(typeof(NpcTemplate))] public TemplateReference rumpolt;
        [SerializeField, TemplateType(typeof(StatusTemplate))] public TemplateReference cheeseStatus;
        [SerializeField, TemplateType(typeof(NpcTemplate))] public List<TemplateReference> wolves;
        [SerializeField, TemplateType(typeof(NpcTemplate))] public List<TemplateReference> summonWolves;
    }
}
