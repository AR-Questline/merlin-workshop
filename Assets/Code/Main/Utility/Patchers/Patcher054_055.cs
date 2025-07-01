using System;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.MVC;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher054_055 : Patcher {
        protected override Version MaxInputVersion => new Version(0, 54, 9999);
        protected override Version FinalVersion => new Version(0, 55);
        
        public override bool AfterDeserializedModel(Model model) =>
            model switch {
                Status status => !status.Template.notSaved,
                _ => true
            };
    }
}