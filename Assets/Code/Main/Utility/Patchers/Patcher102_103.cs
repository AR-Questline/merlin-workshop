using System;
using Awaken.TG.Main.Heroes;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher102_103 : Patcher {
        protected override Version MaxInputVersion => new(1, 2, 9999);
        protected override Version FinalVersion => new(1, 3, 0);

        public override void AfterRestorePatch() {
            var hero = Hero.Current;
            if (hero == null) {
                return;
            }

            if (hero.HeroStats.WyrdWhispers.BaseValue < 0) {
                hero.HeroStats.WyrdWhispers.SetTo(1, false);
            }
        }
    }
}