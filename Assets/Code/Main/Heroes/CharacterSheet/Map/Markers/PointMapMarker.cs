using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public partial class PointMapMarker : SpriteMapMarker {
        protected override Type ViewType => typeof(VPointMapMarker);

        public PointMapMarker(WeakModelRef<IGrounded> groundedRef, Func<string> displayNameGetter, ShareableSpriteReference icon, int order, 
            bool isAlwaysVisible, bool rotate = false, bool highlightAnimation = false)
            : base(groundedRef, displayNameGetter, icon, order, isAlwaysVisible, rotate, highlightAnimation) { }

        public PointMapMarker(WeakModelRef<IGrounded> groundedRef, Func<string> displayNameGetter, MarkerData data, int order,
            bool isAlwaysVisible, bool rotate = false, bool highlightAnimation = false)
            : base(groundedRef, displayNameGetter, data.MarkerIcon, order, isAlwaysVisible, rotate, highlightAnimation) { }
    }
}