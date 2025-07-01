using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public partial class HeroInteractionFocusOverride : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.HeroInteractionFocusOverride;

        public Transform FocusPoint { get; private set; } = null;
        
        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(t => {
                var focusMarker = t.GetComponentInChildren<HeroInteractionFocusOverrideMarker>();
                if (focusMarker != null) {
                    FocusPoint = focusMarker.transform;
                }
            });
        }
    }
}