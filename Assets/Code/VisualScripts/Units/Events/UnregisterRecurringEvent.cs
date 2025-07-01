using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Events {
    [UnitCategory("Events")]
    [UnitOrder(2)]
    [TypeIcon(typeof(CustomEvent))]
    public class UnregisterRecurringEvent : Unit {
        public ControlInput enter;
        public ControlOutput exit;
        
        public ValueInput name;

        protected override void Definition() {
            enter = ControlInput("Enter", Invoke);
            exit = ControlOutput("Exit");
            
            name = ValueInput("Name", "");
            
            Succession(enter, exit);
            
            Requirement(name, enter);
        }

        ControlOutput Invoke(Flow flow) {
            var evtTarget = flow.stack.machine;
            string evtName = flow.GetValue<string>(name);
            World.Services?.TryGet<RecurringActions>()?.UnregisterAction(RecurringEventUtil.ID(evtTarget, evtName));
            return exit;
        }
    }
}