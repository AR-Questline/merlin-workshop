using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Utility.UI.Feedbacks;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    public abstract class VCTabButtonBase<TView, TTabsView> : TreeTabsBase<TView, TTabsView>.VCHeaderTabButton where TTabsView : View where TView : View, IVTalentOverview {
        [SerializeField] VCHighlightFeedback highlightFeedback;
        public override string ButtonName => Type.Tree.Name;
        
        protected override bool ShowRequiredFlag => false;
        protected override bool AdditionalRequirements => string.IsNullOrEmpty(Type.Tree.RequiredFlag) || StoryFlags.Get(Type.Tree.RequiredFlag);
        
        protected override void Refresh(bool selected) {
            base.Refresh(selected);
            
            if (selected) {
                Target.ParentModel.UpdateTreeLevel();
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