using Awaken.Utility;
using System;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Spawners {
    public partial class LifetimeElement : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.LifetimeElement;

        [Saved] DateTime _deathTime;

        TimedEvent _event;

        [JsonConstructor, UnityEngine.Scripting.Preserve] LifetimeElement() { }

        public LifetimeElement(DateTime deathTime) {
            _deathTime = deathTime;
        }
        
        public LifetimeElement(TimeSpan lifetime) {
            _deathTime = World.Only<GameRealTime>().WeatherTime.Date + lifetime;
        }

        protected override void OnInitialize() {
            _event = new TimedEvent(_deathTime, OnDeath);
            World.Only<GameTimeEvents>().AddEvent(_event);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_event != null) {
                World.Only<GameTimeEvents>().RemoveEvent(_event);
                _event = null;
            }
        }

        void OnDeath() {
            _event = null;
            ParentModel.Discard();
        }
    }
}