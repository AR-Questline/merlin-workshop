using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.Components.Navigation;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment {
    public class VCArmorWeightBar : ViewComponent<LoadoutsUI>, IWithTooltip {
        [SerializeField] ExplicitComponentNavigation navigation;
        
        public TooltipConstructor TooltipConstructor {
            get {
                var armorWeight = Hero.Current.Element<ArmorWeight>();
                return new TooltipConstructor().WithMainText(armorWeight.GetDescription());
            }
        }

        public UIResult Handle(UIEvent evt) {
            if (navigation.TryHandle(evt, out var result)) {
                return result;
            }
            
            return UIResult.Ignore;
        }
    }
}