using Awaken.TG.Main.Utility.UI.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    public class VCTalentTreeTabButton : TalentTreeTabs.VCHeaderTabButton {
        [Space(10f)] [SerializeField, InlineProperty, HideLabel]
        TalentTreeTabType type;
        [SerializeField] VCHighlightFeedback highlightFeedback;
        
        public override TalentTreeTabType Type => type;
        public override string ButtonName => type.Tree.Name;
        TalentOverviewUI TalentOverviewUI => Target.ParentModel;
        
        protected override void Refresh(bool selected) {
            base.Refresh(selected);
            
            if (selected) {
                TalentOverviewUI.UpdateTreeLevel();
            } 
            
            RefreshFeedback(selected);
        }
        
        public void RefreshFeedback(bool state) {
            if (state) {
                highlightFeedback.Play();
            } else {
                highlightFeedback.Stop();
            }
        }
    }
}