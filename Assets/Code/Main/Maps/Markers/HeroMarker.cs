using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Markers {
    public partial class HeroMarker : Element<Hero>, ICompassMarker {
        public sealed override bool IsNotSaved => true;

        public bool Enabled => true;
        public bool IgnoreDistanceRequirement => false;
        public Vector3 Position => ParentModel.Coords;
        public string TooltipText => ParentModel.Name;
        public ShareableSpriteReference Icon => null;
        public CompassMarkerType CompassMarkerType => CompassMarkerType.Default;
        [UnityEngine.Scripting.Preserve] public bool IsGreyedOut => false;
        public int OrderNumber => 0;
        public bool IsNumberVisible => false;
        public CompassElement CompassElement => null;
    }
}