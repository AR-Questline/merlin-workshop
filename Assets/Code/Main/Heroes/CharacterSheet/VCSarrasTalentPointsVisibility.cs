using Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class VCSarrasTalentPointsVisibility : ViewComponent {
        [SerializeField] TalentTreeBranchType branchType;
        
        protected override void OnAttach() {
            World.EventSystem.ListenTo(EventSelector.AnySource, VCSarrasTreeBranchButton.Events.TalentTreeBranchClicked, this, OnTalentTreeClicked);
            World.EventSystem.ListenTo(EventSelector.AnySource, TalentTreeUI.Events.TreeZoomedIn, this, OnTreeZoomed);

            OnTalentTreeClicked(TalentTreeBranchType.None);
        }

        void OnTreeZoomed(bool zoomed) {
            if (!zoomed) {
                OnTalentTreeClicked(TalentTreeBranchType.None);
            }
        }

        void OnTalentTreeClicked(TalentTreeBranchType clickedType) {
            gameObject.SetActiveOptimized(branchType == clickedType);
        }
    }
}