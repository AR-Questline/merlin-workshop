using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Stats
{
    /// <summary>
    /// A model-based tweak for a single stat.
    /// </summary>
    public partial class StatTweak : Element<Model>, ITweaker {
        public override ushort TypeForSerialization => SavedModels.StatTweak;

        // === Fields and properties
        bool _isValid = true;

        [Saved] public IWithStats Owner { get; private set; }
        [Saved] public StatType StatType { get; private set; }
        [Saved] public float Modifier { get; private set; }
        [Saved] public TweakPriority Priority { get; private set; }
        [Saved] public OperationType OperationType { get; private set; }
        
        public Stat TweakedStat => _isValid ? Owner.Stat(StatType) : null;
        bool IsOwnerValid => Owner is { HasBeenDiscarded: false };
        
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

        protected override void OnRestore() {
            if (!IsOwnerValid) {
                Log.Important?.Error($"StatTweak {this} restored with an invalid Owner, scheduling cleanup");
                _isValid = false;
                CleanupObsoleteStatTweaks();
                return;
            }
            var tweaks = Services.Get<TweakSystem>();
            tweaks.Tweak(TweakedStat, this, Priority);
        }

        // === Applying and reapplying

        public void ChangeModifier(float delta) {
            Modifier += delta;
            TweakedStat?.RecalculateTweaks();
        }

        public void SetModifier(float value) {
            Modifier = value;
            TweakedStat?.RecalculateTweaks();
        }
        
        public void SwapModifier(float value, TweakPriority tweakPriority, OperationType operationType, bool triggerOwner = true) {
            Modifier = value;
            Priority = tweakPriority;
            OperationType = operationType;
            TweakedStat?.RecalculateTweaks(triggerOwner);
        }

        public float TweakFn(float originalValue, Tweak _) {
            return OperationType.Calculate(originalValue, Modifier);
        }

        static bool s_cleanupOrdered;
        
        public static void CleanupObsoleteStatTweaks() {
            if (s_cleanupOrdered) {
                return;
            }
            DelayedCleanup().Forget();
        }
        
        static async UniTaskVoid DelayedCleanup() {
            s_cleanupOrdered = true;
            if (!await AsyncUtil.DelayFrame(Hero.Current)) {
                s_cleanupOrdered = false;
                return;
            }
            foreach (var statTweak in World.All<StatTweak>().ToArraySlow()) {
                if (!statTweak._isValid || !statTweak.IsOwnerValid) {
                    statTweak.Discard();
                }
            }
            s_cleanupOrdered = false;
        }
    }
}
