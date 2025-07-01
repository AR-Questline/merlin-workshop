using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    public class VCQuickItemTooltipUI : VCStaticItemInfoUI<QuickUseWheelUI, VQuickUseWheelUI> {
        protected override IItemTooltipComponent[] AllSections => new IItemTooltipComponent[] { header, body, effects, gem, buff };
        
        protected override void Initialize() { }

        public void ShowItem(Item item) => ShowDelayed(item).Forget();

        public void HideItem() => Hide();

        async UniTaskVoid ShowDelayed(Item item) {
            if (await AsyncUtil.DelayFrame(Target, 2)) {
                ItemRefreshed(item);
            }
        }
    }
}