using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class GameTimeDuration : Element<IModel> {
        public override ushort TypeForSerialization => SavedModels.GameTimeDuration;

        [Saved] public ARDateTime DestinationTime { get; private set; }

        static ARDateTime GameTime => World.Only<GameRealTime>().WeatherTime;

        [JsonConstructor, UnityEngine.Scripting.Preserve] protected GameTimeDuration() { }

        public GameTimeDuration(ARTimeSpan duration) {
            DestinationTime = GameTime + duration;
        }

        protected override void OnInitialize() {
            World.Only<GameRealTime>().ListenTo(GameRealTime.Events.GameTimeChanged, GameTimeChanged, this);
        }

        void GameTimeChanged(ARDateTime newTime) {
            if (newTime >= DestinationTime) {
                Discard();
            }
        }

        public void Prolong(ARTimeSpan duration) {
            DestinationTime += duration;
        }

        [UnityEngine.Scripting.Preserve]
        public void Renew(ARTimeSpan duration) {
            DestinationTime = GameTime + duration;
        }
    }
}
