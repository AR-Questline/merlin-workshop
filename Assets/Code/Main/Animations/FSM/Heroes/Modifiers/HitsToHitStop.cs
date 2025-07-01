using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Modifiers {
    public class HitsToHitStop : RichEnum {
        public int? HitsRequired { get; }
        protected HitsToHitStop(string enumName, int? hitsRequired) : base(enumName) {
            HitsRequired = hitsRequired;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly HitsToHitStop
            Blunt = new(nameof(Blunt), 1),
            Cut = new(nameof(Cut), 1),
            Stab = new(nameof(Stab), null);
    }
}