using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Quests.UI.Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [UsesPrefab("Quest/" + nameof(VQuestListUI))]
    public class VQuestListUI : View<QuestLogUI>, IUIAware, IAutoFocusBase {
        [SerializeField] List<QuestLogButton> tabButtons;
        [SerializeField] Transform questParent;
        [SerializeField] TextMeshProUGUI barText;
        [SerializeField] Transform sectionsInactiveParent;
        [SerializeField] List<QuestSectionUI> sectionsUI;
        
        int CurrentTabIndex { get; set; }
        
        public Transform QuestParent => questParent;
        public override Transform DetermineHost() => Target.ParentModel.View<VQuestLogUI>().LeftContent;

        // === Initialization
        protected override void OnInitialize() {
            sectionsInactiveParent.TrySetActiveOptimized(false);
            
            for (int index = 0; index < tabButtons.Count; index++) {
                QuestLogButton tab = tabButtons[index];
                tab.TabIndex = index;
                tab.ButtonConfig.InitializeButton(() => OnFilterTabClick(tab));
            }
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, Target));
            CurrentTabIndex = 0;
            SelectFirstTab().Forget();
        }

        public void SetupQuestSection(QuestType questType, int siblingIndex) {
            QuestSectionUI sectionUI = sectionsUI.FirstOrDefault(s => s.QuestType == questType);
            Transform sectionTransform = sectionUI.SectionTransform;
            if (sectionTransform != null) {
                sectionTransform.SetParent(questParent, false);
                sectionTransform.SetSiblingIndex(siblingIndex);
            }
        }
        
        void SetQuestSectionActive(bool active) {
            foreach (QuestSectionUI sectionUI in sectionsUI) {
                sectionUI.SectionTransform.TrySetActiveOptimized(active);
            }
        }

        async UniTaskVoid SelectFirstTab() {
            if (await AsyncUtil.DelayFrame(Target)) {
                SelectTabByIndex(CurrentTabIndex);
            }
        }

        void OnFilterTabClick(QuestLogButton b) {
            ChangeListVisibility(b.QuestListType);
            ChangeColor(b);
            SetBarName(b);
            CurrentTabIndex = b.TabIndex;
            var questUI = Target.Elements<QuestUI>().FirstOrDefault(q => q.IsTracked && q.IsVisible) ??
                          Target.Elements<QuestUI>().FirstOrDefault(q => q.IsVisible);
            questUI?.Select();
            Target.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, questUI != null);
            Target.Trigger(QuestLogUI.Events.QuestSelected, questUI);
            World.Only<Focus>().Select(questUI?.View<VQuestUI>().Button);
        }

        void SetBarName(QuestLogButton button) {
            barText.text = $"{LocTerms.QuestTypeBase.Translate().FontLight().ColoredText(ARColor.MainGrey)} {button.QuestTabName.Italic().FontSemiBold().ColoredText(ARColor.MainWhite)}";
        }
        
        void ChangeColor(QuestLogButton button) {
            foreach (var b in tabButtons) {
                b.ChangeColor(false);
            }
            button.ChangeColor(true);
        }
        
        void ChangeListVisibility(QuestListType listType) {
            SetQuestSectionActive(listType == QuestListType.All);
            
            foreach (QuestUI quest in Target.AllQuests) {
                if (listType == QuestListType.Completed) {
                    quest.ChangeVisibility(quest.IsQuestCompleted);
                } else if (listType == QuestListType.Failed) {
                    quest.ChangeVisibility(quest.IsQuestFailed);
                } else {
                    bool visible = !quest.IsQuestFinished && MatchQuestType(listType, quest.QuestType);
                    quest.ChangeVisibility(visible);
                }
            }
        }

        static bool MatchQuestType(QuestListType listType, QuestType questType) {
            return questType switch {
                QuestType.Main => listType.HasFlagFast(QuestListType.Main),
                QuestType.Side => listType.HasFlagFast(QuestListType.Side),
                QuestType.Misc => listType.HasFlagFast(QuestListType.Misc),
                _ => false
            };
        }

        void SelectNextTab() {
            int nextIndex = (CurrentTabIndex + 1) % tabButtons.Count;
            SelectTabByIndex(nextIndex);
        }
        
        void SelectPreviousTab() {
            int previousIndex = (CurrentTabIndex + tabButtons.Count - 1) % tabButtons.Count;
            SelectTabByIndex(previousIndex);
        }

        void SelectTabByIndex(int index) {
            OnFilterTabClick(tabButtons[index]);
            CurrentTabIndex = index;
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction action) {
                if (action.Name == Target.Next) {
                    SelectNextTab();
                    return UIResult.Accept;
                }
                if (action.Name == Target.Previous) {
                    SelectPreviousTab();
                    return UIResult.Accept;
                }
            }

            return UIResult.Ignore;
        }
    }
}