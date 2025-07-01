using System;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher022_023 : Patcher {
        protected override Version MaxInputVersion => new Version(0, 22);
        protected override Version FinalVersion => new Version(0, 23);
        
        public override bool AfterDeserializedModel(Model model) {
            if (model is Status status) {
                if (status.Template == null || status.Template.IsBuildupAble) {
                    return false;
                }
            }
            return true;
        }
    }
}