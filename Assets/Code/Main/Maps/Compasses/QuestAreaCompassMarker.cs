using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Compasses {
    public partial class QuestAreaCompassMarker : CompassMarker {
        readonly LocationArea _area;

        bool _inArea;

        public QuestAreaCompassMarker(LocationMarker marker, LocationArea area) : base(marker) {
            _area = area;
        }
        
        public override float Distance(Vector3 from) {
            return _area.DistanceTo(from);
        }

        public override AlphaValue CalculateAlpha(Vector3 from) {
            float distanceSq = _area.DistanceSqTo(from);
            if (distanceSq <= 0) {
                EnterSearchArea();
                return AlphaValue.FullyTransparent;
            } else {
                ExitSearchArea();
                return AlphaValue.FullyOpaque;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                ExitSearchArea();
            }
        }

        void EnterSearchArea() {
            if (_inArea) {
                return;
            }
            _inArea = true;
            ParentModel.NotifyEnterSearchArea();
        }

        void ExitSearchArea() {
            if (!_inArea) {
                return;
            }
            _inArea = false;
            ParentModel.NotifyExitSearchArea();
        }
    }
}