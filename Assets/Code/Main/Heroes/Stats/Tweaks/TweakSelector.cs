using System;
using Awaken.TG.MVC;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Stats.Tweaks {
    /// <summary>
    /// Used to target stats by stat type and either target model or type.
    /// </summary>
    public struct TweakSelector : IEquatable<TweakSelector> {
        // Only one is set, either Model or Type
        public IWithStats Model { get; private set; }

        public StatType StatType { get; private set; }

        // Works only for Model Selectors!!!
        [JsonIgnore] public Stat Stat => Model.Stat(StatType);

        public TweakSelector(IWithStats model, StatType statType) {
            this.Model = model;
            this.StatType = statType;
        }

        public override int GetHashCode() {
            unchecked {
                int targetHash = Model.ID.GetHashCode();
                return (targetHash * 397) ^ StatType.GetHashCode();
            }
        }

        public override string ToString() {
            string target = Model.ID;
            return $"{target}|{StatType}";
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TweakSelector && Equals((TweakSelector) obj);
        }

        public bool Equals(TweakSelector other) {
            return string.Equals(Model.ID, other.Model.ID) && string.Equals(StatType.EnumName, other.StatType.EnumName);
        }
    }
}