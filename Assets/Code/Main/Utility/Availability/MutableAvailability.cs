using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Availability {
    [Serializable]
    public class MutableAvailability : AvailabilityBase {
        [SerializeField, InlineProperty, HideLabel] 
        FlagLogic availability;

        IEventListener _flagChangeListener;

        protected override bool SceneInitializationNeeded => availability.HasFlag;

        protected override void OnSceneInitialized() {
            _flagChangeListener = World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(availability.Flag), CheckChanged);
        }
        
        protected override void DisposeListeners() {
            base.DisposeListeners();
            World.EventSystem.TryDisposeListener(ref _flagChangeListener);
        }

        protected override bool CalculateAvailability() {
            if (!World.Services.TryGet<GameplayMemory>(out _)) {
                return false;
            }
            return availability.Get(emptyFlagResult: true);
        }
    }
}