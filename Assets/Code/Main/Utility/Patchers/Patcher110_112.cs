using System;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher110_112 : Patcher {
        protected override Version MaxInputVersion => new(1, 11, 99);
        protected override Version FinalVersion => new(1, 12, 1);

        public override bool AfterDeserializedModel(Model model) {
            if (model is ItemStats itemStats) {
                ResetItemManaStatsDif(itemStats);
            }
            return true;
        }

        void ResetItemManaStatsDif(ItemStats itemStats) {
            var newWrapper = itemStats.Wrapper;
            newWrapper.LightCastManaCostDif = 0;
            newWrapper.HeavyCastManaCostDif = 0;
            newWrapper.HeavyCastManaCostPerSecondDif = 0;
            itemStats.SwapWrapperPreInitialize(newWrapper);
        }
    }
}