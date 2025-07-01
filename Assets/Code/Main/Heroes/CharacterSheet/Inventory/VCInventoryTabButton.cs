using System;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Inventory {
    public class VCInventoryTabButton : InventorySubTabs.VCHeaderTabButton, INewThingContainer {
        [RichEnumExtends(typeof(InventorySubTabType))] 
        [SerializeField] RichEnumReference tabType;
        public override InventorySubTabType Type => tabType.EnumAs<InventorySubTabType>();
        public override string ButtonName => Type.Title;
        public event Action onNewThingRefresh;

        protected override void OnAttach() {
            base.OnAttach();
            World.Services.Get<NewThingsTracker>().RegisterContainer(this);
        }

        public bool NewThingBelongsToMe(IModel model) {
            if (Type == InventorySubTabType.Bag) {
                return model is Item { HiddenOnUI: false, Owner: Hero };
            } 
            
            if (Type == InventorySubTabType.Equipment) {
                return model is Item { HiddenOnUI: false, Owner: Hero, EquipmentType: not null };
            }
            
            return false;
        }
        
        public void RefreshNewThingsContainer() {
            onNewThingRefresh?.Invoke();
        }
        
        protected override void OnDiscard() {
            World.Services.Get<NewThingsTracker>().UnregisterContainer(this);
            base.OnDiscard();
        }
    }
}