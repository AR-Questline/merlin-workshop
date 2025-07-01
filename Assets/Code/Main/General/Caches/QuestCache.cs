using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.General.Caches {
    public class QuestCache : BaseCache {
        static QuestCache s_cache;
        public static QuestCache Get => s_cache ??= BaseCache.LoadFromAssets<QuestCache>("df9c64597a22ef54a9eaa88771fb06c7");
        
        public List<QuestSource> questSources = new();
        
        public override void Clear() {
            questSources.Clear();
        }

        public List<QuestSource> FindSourcesFor(QuestTemplateBase questTemplate) {
            List<QuestSource> list = new();
            
            foreach (var source in questSources) {
                List<QuestChangeData> data = source.data.Where(d => d.QuestTemplate == questTemplate).ToList();
                if (data.Any()) {
                    list.Add(new QuestSource(source.sceneRef, source.scenePath) {
                        data = data,
                        storyGraphTemplate = source.storyGraphTemplate
                    });
                }
            }

            return list;
        }
    }

    [Serializable]
    public class QuestSource : SceneSource {
        [HideInInspector]
        public TemplateReference storyGraphTemplate;
        
        public List<QuestChangeData> data = new();
        
        public QuestSource(GameObject go) : base(go) { }
        public QuestSource(SceneReference sceneRef, string path) : base(sceneRef, path) { }
        
#if UNITY_EDITOR
        [ShowInInspector, PropertyOrder(-1)]
        StoryGraph EDITOR_StoryGraph => storyGraphTemplate?.Get<StoryGraph>();
#endif
    }

    [Serializable]
    public class QuestChangeData {
        [HideInInspector, TemplateType(typeof(QuestTemplate))]
        public TemplateReference questTemplate;
        [UnityEngine.Scripting.Preserve] public string questName;
        [HideInInspector]
        public string objectiveGuid;
        [ShowIf(nameof(HasObjective))] [UnityEngine.Scripting.Preserve] 
        public string objectiveName;
        [LabelText("Type")] [UnityEngine.Scripting.Preserve] 
        public ChangeType changeType;

        QuestTemplate _template;
        public QuestTemplate QuestTemplate => _template ??= questTemplate.Get<QuestTemplate>();
        public bool HasObjective => !string.IsNullOrEmpty(objectiveGuid);
        
        public QuestChangeData(TemplateReference questTemplate, string objectiveGuid, ChangeType changeType = ChangeType.None) : this(questTemplate, changeType) {
            this.objectiveGuid = objectiveGuid;
            using var objectiveSpecs = QuestTemplate.ObjectiveSpecs;
            this.objectiveName = objectiveSpecs.value.FirstOrDefault(o => o.Guid == this.objectiveGuid)?.name;
        }
        
        public QuestChangeData(TemplateReference questTemplate, ChangeType changeType = ChangeType.None) {
            this.questTemplate = questTemplate;
            this.questName = QuestTemplate?.name;
            this.changeType = changeType;
        }

        public enum ChangeType : byte {
            None,
            Start,
            Complete,
            Fail,
            Condition,
        }
    }
}