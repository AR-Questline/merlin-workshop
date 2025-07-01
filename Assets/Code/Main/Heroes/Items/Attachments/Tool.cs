using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class Tool : Element<Item>, IRefreshedByAttachment<ToolAttachment> {
        public override ushort TypeForSerialization => SavedModels.Tool;

        public ToolType Type { get; private set; }
        public bool CanInteractWithLightAttack { get; private set; }
        public bool CanInteractWithHeavyAttack { get; private set; }
        
        public void InitFromAttachment(ToolAttachment spec, bool isRestored) {
            Type = spec.Type;
            CanInteractWithLightAttack = spec.canInteractWithLightAttack;
            CanInteractWithHeavyAttack = spec.canInteractWithHeavyAttack;
        }
    }
}