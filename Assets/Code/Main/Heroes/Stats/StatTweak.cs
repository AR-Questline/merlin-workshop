using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Stats
{
    /// <summary>
    /// A model-based tweak for a single stat.
    /// </summary>
    public partial class StatTweak : Element<Model>, ITweaker {
        public override ushort TypeForSerialization => SavedModels.StatTweak;

        // === Fields and properties

        [Saved] public IWithStats Owner { get; private set; }
        [Saved] public StatType StatType { get; private set; }
        [Saved] public float Modifier { get; private set; }
        [Saved] public TweakPriority Priority { get; private set; }
        [Saved] public OperationType OperationType { get; private set; }
        
        public Stat TweakedStat => Owner.Stat(StatType);
        
        // === Static creators
        public static StatTweak Add(Stat tweakedStat, float modifier, TweakPriority? priority = null, Model parentModel = null) => new StatTweak(tweakedStat, modifier, priority, OperationType.Add, parentModel);
        public static StatTweak AddPreMultiply(Stat tweakedStat, float modifier, TweakPriority? priority = null, Model parentModel = null) => new StatTweak(tweakedStat, modifier, priority, OperationType.AddPreMultiply, parentModel);
        public static StatTweak Multi(Stat tweakedStat, float modifier, TweakPriority? priority = null, Model parentModel = null) => new StatTweak(tweakedStat, modifier, priority, OperationType.Multi, parentModel);
        public static StatTweak Override(Stat tweakedStat, float modifier, TweakPriority? priority = null, Model parentModel = null) => new StatTweak(tweakedStat, modifier, priority, OperationType.Override, parentModel);

        // === Constructors

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        protected StatTweak() { } // deserialization only

        public StatTweak(Stat tweakedStat, float modifier, TweakPriority? priority = null, OperationType operation = null, Model parentModel = null) {
            Owner = tweakedStat.Owner;
            StatType = tweakedStat.Type;
            Modifier = modifier;

            OperationType = operation ?? OperationType.Add;
            Priority = priority ?? OperationType.priority;

            parentModel?.AddElement(this);
        }

        protected override void OnInitialize() {
            var tweaks = Services.Get<TweakSystem>();
            tweaks.Tweak(TweakedStat, this, Priority);
        }

        // === Applying and reapplying

        public void ChangeModifier(float delta) {
            Modifier += delta;
            TweakedStat.RecalculateTweaks();
        }

        public void SetModifier(float value) {
            Modifier = value;
            TweakedStat.RecalculateTweaks();
        }

        public float TweakFn(float originalValue, Tweak _) {
            return OperationType.Calculate(originalValue, Modifier);
        }
    }
}
