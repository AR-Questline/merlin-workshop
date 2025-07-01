using System.Linq;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Sirenix.Utilities;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Stories.Quests.Templates {
#if UNITY_EDITOR
    // ReSharper disable once InconsistentNaming
    public static class EDITOR_QuestTemplateDebug {
        public static void EDITOR_TeleportToQuestGiver(QuestTemplateBase questBase) {
            if (EDITOR_TryGetQuestGiver(questBase, out Location questGiver)) {
                Hero hero = Hero.Current;
                hero.TeleportTo(questGiver.Coords);
            }
        }

        public static void EDITOR_StartQuest(QuestTemplateBase questBase) {
            TemplatesUtil.EDITOR_AssignGuid(questBase, questBase.gameObject);
            Quest quest = new Quest(questBase);
            World.Add(quest);
        }

        public static void EDITOR_TeleportToObjective(QuestTemplateBase questBase) {
            if (EDITOR_TryGetObjectiveModel(questBase, out Objective objective)) {
                Hero hero = Hero.Current;
                if (objective.MarkersData is { Length: > 0 }) {
                    IGrounded objectiveTarget = Objective.GetTargetsFromScene(objective.MarkersData[0]).FirstOrDefault();
                    if (objectiveTarget != null) {
                        Vector3 target = objectiveTarget.Coords;
                        hero.TeleportTo(target);
                    }
                }
            }
        }
        
        public static void EDITOR_ChangeScene(QuestTemplateBase questBase) {
            if (EDITOR_TryGetObjective(questBase, out ObjectiveSpecBase objective)) {
                ScenePreloader.ChangeMap(objective.TargetScene);
            }
        }

        public static string EDITOR_ChangeSceneMessage(QuestTemplateBase questBase) {
            if (EDITOR_TryGetObjective(questBase, out ObjectiveSpecBase objective)) {
                return $"Objective is in scene: {objective.TargetScene.Name}";
            }

            return "Objective is in another scene";
        }

        public static void EDITOR_TryToCompleteObjective(QuestTemplateBase questBase) {
            if (EDITOR_TryGetObjectiveModel(questBase, out Objective objective)) {
                QuestUtils.ChangeObjectiveState(objective, ObjectiveState.Completed);
            }
        }
        
        public static void EDITOR_StartObjective(QuestTemplateBase questBase) {
            if (EDITOR_TryGetObjectiveModel(questBase, out Objective objective)) {
                QuestUtils.ChangeObjectiveState(objective, ObjectiveState.Active);
            }
        }

        public static bool EDITOR_IsSameScene(QuestTemplateBase questBase) {
            if (EDITOR_TryGetObjective(questBase, out ObjectiveSpecBase objective)) {
                return World.Services.Get<SceneService>().ActiveSceneRef == objective.TargetScene;
            }

            return false;
        }

        static bool EDITOR_TryGetQuestGiver(QuestTemplateBase questBase, out Location questGiver) {
            questGiver = null;
            var locations = World.All<Location>();
            foreach (Location location in locations) {
                StoryGraph graph = location.TryGetElement<DialogueAction>()?.Bookmark?.EDITOR_Graph;
                if (graph != null) {
                    foreach (ChapterEditorNode chapter in graph.nodes.OfType<ChapterEditorNode>()) {
                        foreach (EditorStep step in chapter.Elements) {
                            if (step is SEditorQuestAdd questAdd && questAdd.questRef == new TemplateReference(questBase)) {
                                questGiver = location;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        static bool EDITOR_TryGetObjectiveModel(QuestTemplateBase questBase, out Objective objective) {
            objective = null;
            if (EDITOR_TryGetObjective(questBase, out ObjectiveSpecBase objectiveSpec)) {
                Quest quest = World.Any<Quest>(q => q.Template == questBase);
                if (quest == null) {
                    Log.Important?.Error("Quest is not active");
                } else {
                    objective = quest.Objectives.FirstOrDefault(o => o.Guid == objectiveSpec.Guid);
                    if (objective != null) {
                        return true;
                    } else {
                        Log.Important?.Error("Couldn't find chosen objective");
                    }
                }
            }
            return false;
        }

        static bool EDITOR_TryGetObjective(QuestTemplateBase questBase, out ObjectiveSpecBase objective) {
            objective = null;
            if (!questBase._EDITOR_chosenObjective.IsNullOrWhitespace()) {
                using var objectiveSpecs = questBase.ObjectiveSpecs;
                objective = objectiveSpecs.value.FirstOrDefault(o => o.name == questBase._EDITOR_chosenObjective);
            }

            return objective != null;
        }
    }
#endif
}