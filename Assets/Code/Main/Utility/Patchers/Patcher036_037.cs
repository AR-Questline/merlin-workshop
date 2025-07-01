using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher036_037 : Patcher {
        const string GUIDBroadAxe = "7eca5d2e7a9283d4d9c20ef5bed5155f";
        const string GUIDRustyBroadAxe = "2133d83737c072f4fa2ae3054941ee1d";

        protected override Version MaxInputVersion => new Version(0, 36);
        protected override Version FinalVersion => new Version(0, 37);

        public override void AfterRestorePatch() {
            foreach (var loadout in World.All<HeroLoadout>()) {
                Item primary = loadout.PrimaryItem;
                Item secondary = loadout.SecondaryItem;

                if (primary?.Template.GUID is GUIDBroadAxe or GUIDRustyBroadAxe) {
                    loadout.Unequip(primary);
                } 
                
                if (secondary?.Template.GUID is GUIDBroadAxe or GUIDRustyBroadAxe) {
                    loadout.Unequip(secondary);
                }
            }
        }
    }
}