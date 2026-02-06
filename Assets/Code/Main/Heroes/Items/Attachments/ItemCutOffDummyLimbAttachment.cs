using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "Marks item as cut-off limb.")]
    public class ItemCutOffDummyLimbAttachment : MonoBehaviour, IAttachmentSpec {
        [ValidateInput(nameof(IsLimbDataCorrect), "Limb Data is not correct. You can't cut off limb in 2 places at once.")]
        public LimbData limbData;
        
        public Element SpawnElement() {
            return new ItemCutOffDummyLimb();
        }

        public bool IsMine(Element element) {
            return element is ItemCutOffDummyLimb;
        }
        
        bool IsLimbDataCorrect() {
            if (limbData.HasFlagFast(LimbData.LeftArm) && limbData.HasFlagFast(LimbData.LeftForeArm)) {
                return false;
            }
            if (limbData.HasFlagFast(LimbData.RightArm) && limbData.HasFlagFast(LimbData.RightForeArm)) {
                return false;
            }
            if (limbData.HasFlagFast(LimbData.LeftLeg) && limbData.HasFlagFast(LimbData.LeftForeLeg)) {
                return false;
            }
            if (limbData.HasFlagFast(LimbData.RightLeg) && limbData.HasFlagFast(LimbData.RightForeLeg)) {
                return false;
            }
            return true;
        }
    }
}