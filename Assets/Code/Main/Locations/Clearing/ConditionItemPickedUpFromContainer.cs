using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Clearing {
    [UnityEngine.Scripting.Preserve]
    public class ConditionItemPickedUpFromContainer : Condition {
        [SerializeField, DisableInPlayMode, Indent] LocationSpec location;
        [SerializeField, DisableInPlayMode, Indent, TemplateType(typeof(ItemTemplate))] TemplateReference item;
        
        Location Location => World.ByID<Location>(location.GetLocationId());
        ItemTemplate ItemTemplate => item.Get<ItemTemplate>();
        
        protected override void Setup() {
            if (Location == null) {
                Fulfill();
            } else if (!Location.TryGetElement(out SearchAction search)) {
                Log.Important?.Error($"Cannot find SearchAction in {Location}", Location.Spec);
                Fulfill();
            } else if (!search.Contains(ItemTemplate)) {
                Fulfill();
            } else {
                Reference<IEventListener> listenerReference = new();
                listenerReference.item = Location.ListenTo(Location.Events.ItemPickedFromLocation, item => {
                    if (item != null && item.Template == ItemTemplate && !search.Contains(ItemTemplate)) {
                        Fulfill();
                        World.EventSystem.RemoveListener(listenerReference.item);
                    }
                }, Owner);
            }
        }
    }
}
