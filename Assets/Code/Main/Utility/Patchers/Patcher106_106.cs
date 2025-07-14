using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher106_106 : Patcher {
        protected override Version MaxInputVersion => new(1, 6, 5);
        protected override Version FinalVersion => new(1, 6, 5);

        public override void AfterRestorePatch() {
            var context = World.Services.TryGet<GameplayMemory>()?.Context();
            
            if (context == null)
                return;
            
            // disable prologue special case handling for players that have already completed the prologue
            // - player:survivor was set if player skiped prologue
            // - items:basicItemsGiven was set when player went through prologue
            if (context.HasValue("player:survivor") || context.HasValue("items:basicItemsGiven")) {
                context.Set("prologue:initialFordwellerDialogueComplete", true);
            }
        }
    }
}