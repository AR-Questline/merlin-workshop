using System;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Fights.DamageInfo {
    [Serializable]
    public partial class HeroDamageTimestamp {
        public ushort TypeForSerialization => SavedTypes.HeroDamageTimestamp;

        [Saved] public Hero Hero { get; private set; }
        [Saved] public ARTimeSpan DateTime { get; private set; }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        HeroDamageTimestamp() {}

        public HeroDamageTimestamp(Hero hero) {
            Hero = hero;
            DateTime = World.Any<GameRealTime>().PlayRealTime;
        }

        public bool CountAsHeroKill() {
            float timestamp = World.Services.Get<GameConstants>().minutesTimestampToCountKill;
            var current = World.Any<GameRealTime>().PlayRealTime;
            return (current - DateTime).TotalMinutes < timestamp;
        }
    }
}