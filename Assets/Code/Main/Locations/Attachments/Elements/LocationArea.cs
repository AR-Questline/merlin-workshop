using System;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public abstract partial class LocationArea : Element<Location> {
        [UnityEngine.Scripting.Preserve] public abstract Type CompassMarkerView { get; }
        public abstract Type MapMarkerView { get; }
        public abstract float DistanceSqTo(Vector3 point);
        public abstract float DistanceTo(Vector3 point);
    }
}