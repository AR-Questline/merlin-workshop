using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher101_102 : Patcher {
        protected override Version MaxInputVersion => new(1, 1, 9999);
        protected override Version FinalVersion => new(1, 2, 0);

        public override void AfterRestorePatch() {
            var gameplayMemory = World.Services.TryGet<GameplayMemory>();
            if (gameplayMemory != null && gameplayMemory.Context().HasValue("neante:ritualdone")) {
                gameplayMemory.Context().Set("neante:ritualdone", true);
            }
        }
    }
}