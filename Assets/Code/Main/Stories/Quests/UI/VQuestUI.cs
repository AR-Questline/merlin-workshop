using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [UsesPrefab("Quest/" + nameof(VQuestUI))]
    public class VQuestUI : View<QuestUI> {
        [SerializeField] TextMeshProUGUI questTitle;
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] Image newQuestImage;
        [SerializeField] Image questIcon;
        [SerializeField] GameObject trackedImage;

        public ARButton Button => buttonConfig.button;
        public bool IsVisible { get; private set; }

        public override Transform DetermineHost() => Target.ParentModel.View<VQuestListUI>().QuestParent;

        public void ChangeVisibility(bool visible) {
            IsVisible = visible;
            gameObject.SetActive(visible);
        }

        protected override void OnInitialize() {
            buttonConfig.InitializeButton(() => Target.Select());
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.FocusChanged, this, OnFocusChange);
            Target.ParentModel.ListenTo(QuestLogUI.Events.QuestSelected, Refresh);
            Refresh();
        }

        void Refresh() {
            Quest quest = Target.QuestData;
            newQuestImage.enabled = !quest.DisplayedByPlayer;
            
            Color color = ARColor.MainGrey;
            string title = quest.DisplayName;
            trackedImage.SetActive(Target.IsTracked);
            if (!quest.DisplayedByPlayer) {
                color = ARColor.MainWhite;
            }
            if (Target.IsSelected) {
                color = ARColor.MainAccent;
            }

            FactionTemplate templateRelatedFaction = quest.Template.RelatedFaction;

            if (quest.Template.iconDescriptionReference is { IsSet: true }) {
                quest.Template.iconDescriptionReference.TryRegisterAndSetup(this, questIcon);
            } else if (templateRelatedFaction is { iconReference: { IsSet: true } }) {
                templateRelatedFaction.iconReference.TryRegisterAndSetup(this, questIcon);
            }
            
            questTitle.color = color;
            questTitle.text = title;
            buttonConfig.SetSelection(Target.IsSelected);
        }

        void OnFocusChange(FocusChange change) {
            if (change.current == buttonConfig.button) {
                if (!Target.IsSelected) {
                    Target.Select();
                }
            }
        }
    }
}
