using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Markers {
    public interface ICompassMarker : IModel {
        bool Enabled { get; }
        bool IgnoreDistanceRequirement { get; }
        Vector3 Position { get; }
        string TooltipText { get; }
        ShareableSpriteReference Icon { get; }
        CompassMarkerType CompassMarkerType { get; }
        int OrderNumber { get; }
        bool IsNumberVisible { get; }
        CompassElement CompassElement { get; }

        public static class Events {
            public static readonly Event<ICompassMarker, bool> EnabledChanged = new(nameof(EnabledChanged));
            public static readonly Event<ICompassMarker, CompassMarkerType> TypeChanged = new(nameof(TypeChanged));

            [UnityEngine.Scripting.Preserve] public static readonly Event<ICompassMarker, bool> MobilityChanged = new(nameof(MobilityChanged));
            [UnityEngine.Scripting.Preserve] public static readonly Event<ICompassMarker, Vector3> PositionChanged = new(nameof(PositionChanged));
        }
    }

    public interface ICompassMarker<out T> : ICompassMarker where T : CompassElement {
        new T CompassElement { get; }
        CompassElement ICompassMarker.CompassElement => CompassElement;
    }
}