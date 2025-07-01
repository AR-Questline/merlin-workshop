using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class PassiveStatModifierWithListener : PassiveStatModifier {
        public IEventListener listener;
        
        public PassiveStatModifierWithListener(Stat stat, TweakPriority type, float value) : base(stat, type, value) { }
    }
}