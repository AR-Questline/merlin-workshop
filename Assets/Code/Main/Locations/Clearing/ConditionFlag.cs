using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Clearing {
    [UnityEngine.Scripting.Preserve]
    public class ConditionFlag : Condition {
        [SerializeField, DisableInPlayMode, Indent, Tags(TagsCategory.Flag)] string flag;
        
        protected override void Setup() {
            var facts = World.Services.Get<GameplayMemory>().Context();
            if (facts.Get<bool>(flag)) {
                Fulfill();
            } else {
                Reference<IEventListener> listenerReference = new();
                listenerReference.item = World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.FlagChanged, Owner, f => {
                    if (f == flag && facts.Get<bool>(flag)) {
                        Fulfill();
                        World.EventSystem.RemoveListener(listenerReference.item);
                    }
                });
            }
        }
    }
}
