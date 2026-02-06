using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For sarras sickle.")]
    public class SarrasSickleAttachment : ToolAttachment {
        public int initialCharges = 1;
        public int maxCharges = 3;
        public float chargeIncrementPerKill = 0.2f;
        
        public override Element SpawnElement() {
            return new SarrasSickle();
        }

        public override bool IsMine(Element element) => element is SarrasSickle;
    }
}