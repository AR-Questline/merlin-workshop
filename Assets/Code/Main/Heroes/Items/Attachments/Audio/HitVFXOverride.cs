using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    public partial class HitVFXOverride : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        public ShareableARAssetReference HitVFX { get; }
        
        public HitVFXOverride(ShareableARAssetReference hitVFX) {
            HitVFX = hitVFX;
        }
    }
}