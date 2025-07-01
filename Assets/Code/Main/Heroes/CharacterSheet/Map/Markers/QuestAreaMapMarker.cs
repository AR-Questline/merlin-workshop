using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public partial class QuestAreaMapMarker : SpriteMapMarker {
        public LocationArea Area { get; }
        public bool ShowArea { get; }

        protected override Type ViewType => Area.MapMarkerView;

        public QuestAreaMapMarker(LocationArea area, Func<string> displayName, ShareableSpriteReference icon, int order, bool showArea, bool highlightAnimation = false) : 
            base(new WeakModelRef<IGrounded>(area.ParentModel), displayName, icon, order, true, highlightAnimation: showArea) {
            Area = area;
            ShowArea = showArea;
        }
    }
}