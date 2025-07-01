using System;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Bag;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs {
    public class VCItemTabButton : ItemsTabs.VCSelectableTabButton, INewThingContainer {
        [SerializeField, RichEnumExtends(typeof(ItemsTabType))] RichEnumReference type;

        public override ItemsTabType Type => type.EnumAs<ItemsTabType>();
        public event Action onNewThingRefresh;
        
        protected override void OnAttach() {
            base.OnAttach();
            World.Services.Get<NewThingsTracker>().RegisterContainer(this);
        }

        protected override void OnDiscard() {
            World.Services.Get<NewThingsTracker>().UnregisterContainer(this);
            base.OnDiscard();
        }

        // --- INewThing
        public bool NewThingBelongsToMe(IModel model) {
            return Target.ParentModel?.GenericParentModel is BagUI
                && Type != ItemsTabType.All && model is Item {HiddenOnUI: false, Owner: Hero} item && Type.Contains(item);
        }

        public void RefreshNewThingsContainer() {
            onNewThingRefresh?.Invoke();
        }
    }
}