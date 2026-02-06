using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher113_114 : Patcher {
        protected override Version MaxInputVersion => new(1, 13, 99);
        protected override Version FinalVersion => new(1, 14, 1);

        public override void AfterGameLoadedPatch() {
            var hero = Hero.Current;
            if (hero == null) {
                Debug.LogException(new Exception("Hero.Current is null in AfterGameLoadedPatch of " + nameof(Patcher113_114)));
                return;
            }

            foreach (var item in hero.Inventory.ItemInSlots.EquippedItems()) {
                if (item.HasElement<ItemEquip>()) {
                    continue;
                }
                hero.Inventory.Unequip(item);
            }
        }
    }
}