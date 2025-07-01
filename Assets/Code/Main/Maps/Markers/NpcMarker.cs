using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.MVC;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Maps.Markers {
    public sealed partial class NpcMarker : LocationMarker {
        public override ushort TypeForSerialization => SavedModels.NpcMarker;

        bool _inCombat;
        protected override bool IsVisibleUnderFogOfWar => false;
        public override CompassMarkerType CompassMarkerType => _inCombat ? CompassMarkerType.CombatAI : base.CompassMarkerType;

        public void Update(bool inCombat = false, bool isHostile = false, bool isFriendly = false) {
            var data = (NpcMarkerData) MarkerData;
            SetIcon((inCombat, isHostile, isFriendly) switch {
                (true, _, _) => data.CombatMarkerIcon,
                (_, true, _) => data.HostileMarkerIcon,
                (_, _, true) => data.FriendlyMarkerIcon,
                _ => data.MarkerIcon
            });
            if (_inCombat != inCombat) {
                _inCombat = inCombat;
                this.Trigger(ICompassMarker.Events.TypeChanged, CompassMarkerType);
            }
        }

        protected override CompassMarker SpawnCompassMarker() => new NpcCompassMarker(this);
    }
}