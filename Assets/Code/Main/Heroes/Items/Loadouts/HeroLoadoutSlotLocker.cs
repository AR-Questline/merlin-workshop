using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.Loadouts {
    public partial class HeroLoadoutSlotLocker : Element<HeroLoadout> {
        public override ushort TypeForSerialization => SavedModels.HeroLoadoutSlotLocker;

        [Saved] public EquipmentSlotType SlotTypeLocked { get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public HeroLoadoutSlotLocker() {}
        
        public HeroLoadoutSlotLocker(EquipmentSlotType slotTypeLocked) {
            this.SlotTypeLocked = slotTypeLocked;
        }
    }
}