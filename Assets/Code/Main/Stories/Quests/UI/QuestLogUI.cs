using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Stories.Quests.UI {
    /// <summary>
    /// Represents and controls quests ui in character sheet 
    /// </summary>
    public partial class QuestLogUI : QuestLogSubTab<VQuestLogUI>, IKeyProvider<VCTabSwitchKeyIcon.TabSwitch> {
        Prompt _trackQuestPrompt;
        
        public KeyBindings Previous => KeyBindings.UI.Generic.DecreaseValueAlt;
        public KeyBindings Next => KeyBindings.UI.Generic.IncreaseValueAlt;
        
        // === Queries
        public QuestUI SelectedQuest => Elements<QuestUI>().FirstOrDefault(q => q.IsSelected);
        public ModelsSet<QuestUI> AllQuests => Elements<QuestUI>();
        QuestCategory Category { get; set; }
        CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;
        
        public new static class Events {
            public static readonly Event<QuestLogUI, QuestUI> QuestSelected = new(nameof(QuestSelected));
        }

        public QuestLogUI(QuestCategory category) {
            Category = category;
        }

        // === Initialization
        protected override void AfterViewSpawned(VQuestLogUI view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            var questListUI = World.SpawnView<VQuestListUI>(this, forcedParent: view.LeftContent);
            World.SpawnView<VQuestDescriptionUI>(this, forcedParent: view.RightContent);

            var allInOrder = World.AllInOrderReadonlyNotValidated();
            var allQuests = new List<Quest>(64);
            foreach (var item in allInOrder) {
                if (item is Quest { HasBeenDiscarded: false, VisibleInQuestLog: true } quest && quest.Category == Category) {
                    allQuests.Add(quest);
                }
            }
            allQuests.Sort((l, r) => ((int)l.QuestType).CompareTo((int)r.QuestType));

            QuestType? questType = null;
            int sectionIndexInHierarchy = 0;
            foreach (Quest quest in allQuests) {
                if (quest.State == QuestState.Active && questType != quest.QuestType) {
                    questType = quest.QuestType;
                    questListUI.SetupQuestSection(questType.Value, sectionIndexInHierarchy);
                    sectionIndexInHierarchy++;
                }
                
                var questUI = new QuestUI(quest);
                AddElement(questUI);
                sectionIndexInHierarchy++;
            }

            allQuests.Clear();

            _trackQuestPrompt = CharacterSheetUI.Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Accept, LocTerms.QuestNotificationTrack.Translate(), SetTrackedQuest), this, false);
            this.ListenTo(Events.QuestSelected, OnQuestSelected, this);
        }

        void OnQuestSelected(QuestUI questUI) {
            _trackQuestPrompt.SetActive(questUI is { IsSelected: true, IsTracked: false });
        }

        public void SetTrackedQuest(Quest questData) {
            QuestTracker questTracker = World.Only<QuestTracker>();
            questTracker.Track(questData);
            _trackQuestPrompt.SetActive(false);
        }

        void SetTrackedQuest() {
            var questUI = Elements<QuestUI>().FirstOrDefault(questUi => questUi.IsSelected);
            if (questUI != null) {
                SetTrackedQuest(questUI.QuestData);
            }
        }

        public KeyIcon.Data GetKey(VCTabSwitchKeyIcon.TabSwitch key) {
            return key switch {
                VCTabSwitchKeyIcon.TabSwitch.Next => new(Next, false),
                VCTabSwitchKeyIcon.TabSwitch.Previous => new(Previous, false),
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
            };
        }
    }
}
