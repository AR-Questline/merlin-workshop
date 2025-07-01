using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Templates;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Structure {
    public interface IDataViewSource {
        public Object UnityObject { get; }
        public string Name { get; }
        public string Id { get; }
    }

    public class DataViewSource : IDataViewSource {
        public Object UnityObject { get; }
        public string Name => UnityObject.name;
        public virtual string Id => $"Object:{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(UnityObject))}";

        public DataViewSource(Object unityObject) {
            UnityObject = unityObject;
        }
    }

    public class DataViewQuestSource : DataViewSource {
        QuestTemplateBase QuestTemplate { get; }
        
        public override string Id => $"Quest:{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(QuestTemplate))}";
        
        public DataViewQuestSource(QuestTemplateBase questTemplate) : base(questTemplate) {
            QuestTemplate = questTemplate;
        }
    }
    
    public class DataViewQuestObjectiveSource : DataViewSource {
        QuestTemplateBase QuestTemplate { get; }

        public override string Id => $"Quest:{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(QuestTemplate))}:{((ObjectiveSpec)UnityObject).Guid}";

        public DataViewQuestObjectiveSource(QuestTemplateBase questTemplate, ObjectiveSpecBase objectiveSpec) : base(objectiveSpec) {
            QuestTemplate = questTemplate;
        }
    }

    public class DataViewLootDataSource : IDataViewSource {
        public ItemTemplate ItemTemplate { get; }
        public int SceneIndex { get; }
        public int SourceIndex { get; }
        public int LootIndex { get; }

        public Object UnityObject => ItemTemplate.gameObject;
        public string Name => ItemTemplate.name;
        public string Id => $"Loot:{SceneIndex}:{SourceIndex}:{LootIndex}";

        public DataViewLootDataSource(ItemTemplate template, int sceneIndex, int sourceIndex, int lootIndex) {
            ItemTemplate = template;
            SceneIndex = sceneIndex;
            SourceIndex = sourceIndex;
            LootIndex = lootIndex;
        }
    }
}