using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCSarrasSlotsHandler : ViewComponent<Hero> {
        [SerializeField] Transform topRowParent;
        [SerializeField] Transform bottomRowParent;
        [SerializeField] Transform sickleSlotTransform;
        [SerializeField] Transform sarrasTreeBranchesTransform;
        [SerializeField] Transform sickleChargesTransform;
        [SerializeField] Transform sickleChargesParentTop;
        [SerializeField] Transform sickleChargesParentBottom;
        
        SarrasHeroTreeBranches _sarrasHeroTreeBranches;

        protected override void OnAttach() {
            Target.AfterFullyInitialized(() => {
                _sarrasHeroTreeBranches = Target.Development.SarrasHeroTreeBranches;
                HandleSlotParents();
                if (!_sarrasHeroTreeBranches.IsFirstCharged) {
                    _sarrasHeroTreeBranches.ListenToLimited(SarrasHeroTreeBranches.Events.FirstChargeCommitted, HandleSlotParents, this);
                }
            });
        }

        void HandleSlotParents() {
            bool isFirstCharged = _sarrasHeroTreeBranches.IsFirstCharged;
            sickleSlotTransform.SetParent(isFirstCharged ? topRowParent : bottomRowParent, false);
            sarrasTreeBranchesTransform.SetParent(bottomRowParent, false);
            sickleChargesTransform.SetParent(isFirstCharged ? sickleChargesParentTop : sickleChargesParentBottom, false);
        }
    }
}