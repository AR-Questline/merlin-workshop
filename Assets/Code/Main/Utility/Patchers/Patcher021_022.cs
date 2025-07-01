using System;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher021_022 : Patcher {
        protected override Version MaxInputVersion => new Version(0, 21);
        protected override Version FinalVersion => new Version(0, 22);
        
        public override bool AfterDeserializedModel(Model model) {
            if (model is HeroStats heroStats) {
                heroStats.EncumbranceLimit.IncreaseBy(50);
            }
            return true;
        }
    }
}