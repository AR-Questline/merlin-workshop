using System;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher112_113 : Patcher {
        protected override Version MaxInputVersion => new(1, 12, 9999);
        protected override Version FinalVersion => new(1, 13, 1);

        public override void AfterGameLoadedPatch() {
            Patcher_ItemUpgradeReverting.Patch113.Apply();
        }
    }
}