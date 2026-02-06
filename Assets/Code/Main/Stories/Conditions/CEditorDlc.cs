using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("DLC: Check DLC"), NodeSupportsOdin]
    public class CEditorDlc : EditorCondition {
        public DlcCategory dlcCategory;
        
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CDlc {
                dlcCategory = dlcCategory
            };
        }
    }
    
    public partial class CDlc : StoryCondition {
        public DlcCategory dlcCategory;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return SocialService.Get.HasDlc(dlcCategory);
        }
    }
}