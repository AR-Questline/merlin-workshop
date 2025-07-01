using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.Utility;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class LocationAreaSphere : LocationArea, IRefreshedByAttachment<LocationAreaSphereAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationAreaSphere;

        LocationAreaSphereAttachment _spec;

        public override Type CompassMarkerView => typeof(VAreaSphereCompassElement);
        public override Type MapMarkerView => typeof(VQuestAreaSphereMapMarker);

        public float Radius => _spec.Radius;
        
        public void InitFromAttachment(LocationAreaSphereAttachment spec, bool isRestored) {
            _spec = spec;
        }
        
        public override float DistanceSqTo(Vector3 point) {
            return ParentModel.Coords.SquaredDistanceTo(point) - math.square(_spec.Radius);
        }

        public override float DistanceTo(Vector3 point) {
            return math.sqrt(math.max(DistanceSqTo(point), 0));
        }
    }
}