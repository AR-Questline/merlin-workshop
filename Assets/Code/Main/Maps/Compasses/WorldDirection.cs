using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Compasses {
    public partial class WorldDirection : CompassElement {
        public sealed override bool IsNotSaved => true;

        Vector3 CardinalDirection { get; }

        public override bool ShouldBeDisplayed => true;
        public override ShareableSpriteReference Icon { get; }
        public override string TopText => string.Empty;
        public override int OrderNumber => 0;
        public override bool IsNumberVisible => false;

        public WorldDirection(Vector3 direction, ShareableSpriteReference icon) : base(
            enabled: true,
            ignoreDistanceRequirement: true,
            ignoreAngleRequirement: false
        ) {
            CardinalDirection = direction;
            Icon = icon;
        }

        public override Vector3 Direction(Vector3 from) => CardinalDirection;
        public override AlphaValue CalculateAlpha(Vector3 from) {
            return AlphaValue.FullyOpaque;
        }
    }
}