using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Gameplay {
    public class AutoSaveFrequency : RichEnum {
        public float Interval { get; }

        public static readonly AutoSaveFrequency
            OneMinute = new(nameof(OneMinute), 60),
            ThreeMinutes = new(nameof(ThreeMinutes), 180),
            FiveMinutes = new(nameof(FiveMinutes), 300),
            TenMinutes = new(nameof(TenMinutes), 600);

        protected AutoSaveFrequency(string enumName, float interval) : base(enumName, nameof(AutoSaveFrequency)) {
            Interval = interval;
        }
    }
}
