using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public abstract partial class SpriteMapMarker : MapMarker {
        public ShareableSpriteReference Icon { get; protected set; }
        
        public SpriteMapMarker(WeakModelRef<IGrounded> groundedRef, Func<string> displayNameGetter, ShareableSpriteReference icon, int order, 
            bool isAlwaysVisible, bool rotate = false, bool highlightAnimation = false)
            : base(groundedRef, displayNameGetter, order, isAlwaysVisible, rotate, highlightAnimation) {
            Icon = icon;
        }

        public SpriteMapMarker(WeakModelRef<IGrounded> groundedRef, Func<string> displayNameGetter, MarkerData data, int order, 
            bool isAlwaysVisible, bool rotate = false, bool highlightAnimation = false)
            : base(groundedRef, displayNameGetter, order, isAlwaysVisible, rotate, highlightAnimation) {
            Icon = data.MarkerIcon;
        }
    }
}