using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Debugging.DebugSetupComponents;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Debugging {
    [UsesPrefab("Debug/VDebugStartStory")]
    public class VDebugStartStory : View<DebugStartStory> {
        
        // === References
        public GameObject flagPrefab;
        public GameObject variablePrefab;
        public GameObject hasItemPrefab;
        public GameObject hasStatsPrefab;
        public GameObject objectivePrefab;
        public GameObject questPrefab;

        public Transform verticalParent;

        public ARButton applyButton;
        public ARButton cancelButton;

        bool _createdAny;
        
        // === Initialization
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            applyButton.OnClick += Apply;
            cancelButton.OnClick += Cancel;
            CreateDebug(Target.Story.Graph);
            
            flagPrefab.SetActive(false);
            variablePrefab.SetActive(false);
            hasItemPrefab.SetActive(false);
            hasStatsPrefab.SetActive(false);
            objectivePrefab.SetActive(false);
            questPrefab.SetActive(false);
        }

        void Start() {
            if (!_createdAny) {
                Target.Discard();
            }
        }

        // === Spawn Logic
        public void CreateDebug(in StoryGraphRuntime graph) {
            List<StoryStep> elements = graph.chapters.SelectMany(n => n.steps).ToList();
            foreach (CEditorFlag flag in elements.OfType<CEditorFlag>()) {
                CreateFlag(flag);
                _createdAny = true;
            }
            foreach (CEditorVariable variable in elements.OfType<CEditorVariable>()) {
                CreateVariable(variable);
                _createdAny = true;
            }

            if (Target.Story.Hero != null) {
                foreach (CEditorHasItems hasItems in elements.OfType<CEditorHasItems>()) {
                    foreach (var pair in hasItems.requiredItemTemplateReferenceQuantityPairs) {
                        CreateHasItem(pair);
                        _createdAny = true;
                    }

                    if (hasItems.tags?.Any() ?? false) {
                        CreateHasItemWithTags(hasItems);
                    }
                }
            }

            foreach (CEditorHasStats hasStats in elements.OfType<CEditorHasStats>()) {
                CreateHasStat(hasStats);
            }

            foreach (CEditorQuestObjective questObjective in elements.OfType<CEditorQuestObjective>()) {
                CreateObjective(questObjective);
            }
            
            foreach (CEditorQuestState questObjective in elements.OfType<CEditorQuestState>()) {
                CreateQuest(questObjective);
            }
        }

        void CreateFlag(CEditorFlag element) {
            DebugSetFlag setFlag = Instantiate(flagPrefab, verticalParent, false).GetComponent<DebugSetFlag>();
            setFlag.gameObject.SetActive(true);
            setFlag.Init(Target.Story, element);
        }

        void CreateVariable(CEditorVariable element) {
            DebugSetVariable setVariable = Instantiate(variablePrefab, verticalParent, false).GetComponent<DebugSetVariable>();
            setVariable.gameObject.SetActive(true);
            setVariable.Init(Target.Story, element);
        }

        void CreateHasItem(ItemSpawningData pair) {
            DebugSetItem setItem = Instantiate(hasItemPrefab, verticalParent, false).GetComponent<DebugSetItem>();
            setItem.gameObject.SetActive(true);
            setItem.Init(Target.Story, pair.ItemTemplate(Target.Story), pair.quantity);
        }
        
        void CreateHasItemWithTags(CEditorHasItems element) {
            DebugSetItem setItem = Instantiate(hasItemPrefab, verticalParent, false).GetComponent<DebugSetItem>();
            setItem.gameObject.SetActive(true);
            setItem.Init(Target.Story, element.tags, element.tagsQuantity);
        }
        
        void CreateHasStat(CEditorHasStats editorHasStats) {
            DebugSetStat setStat = Instantiate(hasStatsPrefab, verticalParent, false).GetComponent<DebugSetStat>();
            setStat.gameObject.SetActive(true);
            setStat.Init(Target.Story, editorHasStats);
        }
        
        void CreateObjective(CEditorQuestObjective element) {
            DebugSetObjective setObjective = Instantiate(objectivePrefab, verticalParent, false).GetComponent<DebugSetObjective>();
            setObjective.gameObject.SetActive(true);
            setObjective.Init(Target.Story, element);
        }     
        
        void CreateQuest(CEditorQuestState element) {
            DebugSetQuest setQuest = Instantiate(questPrefab, verticalParent, false).GetComponent<DebugSetQuest>();
            setQuest.gameObject.SetActive(true);
            setQuest.Init(Target.Story, element);
        }
        
        // === Callbacks

        void Apply() {
            foreach (IDebugComponent comp in GetComponentsInChildren<IDebugComponent>(false)) {
                comp.Apply(Target.Story);
            }
            Target.Discard();
        }

        void Cancel() {
            Target.Discard();
        }
    }
}