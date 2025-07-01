using System;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher040_041 : Patcher_RestoreOnFastTravel {
        protected override Version MaxInputVersion => new Version(0, 40);
        protected override Version FinalVersion => new Version(0, 41);
        
        public Patcher040_041() : base(new[] {
            CampaignMap
        }) { }
    }
}