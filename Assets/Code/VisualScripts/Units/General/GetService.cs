using System;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General")]
    [TypeIcon(typeof(IService))]
    [UnityEngine.Scripting.Preserve]
    public class GetService : Unit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public Type type;
        
        protected override void Definition() {
            ValueOutput("service", _ => World.Services.Get(type));
        }
    }
}