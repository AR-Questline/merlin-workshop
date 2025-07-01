using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Events {
    [UnitCategory("Events")]
    [UnitOrder(2)]
    [TypeIcon(typeof(CustomEvent))]
    public class RegisterRecurringEvent : Unit {
        
        public ControlInput enter;
        public ControlOutput exit;
        
        public ValueInput name;
        public ValueInput interval;
        
        protected override void Definition() {
            enter = ControlInput("Enter", Invoke);
            exit = ControlOutput("Exit");
            
            name = ValueInput("Name", "");
            interval = ValueInput("Interval", 1F);

            Succession(enter, exit);
            
            Requirement(name, enter);
            Requirement(interval, enter);
        }

        ControlOutput Invoke(Flow flow) {
            var evtTarget = flow.stack.machine;
            string evtName = flow.GetValue<string>(name);
            float evtInterval = flow.GetValue<float>(interval);
            RegisterRecurringAction(evtTarget, evtName, evtInterval).Forget();
            return exit;
        }

        static async UniTaskVoid RegisterRecurringAction(IMachine evtTarget, string evtName, float evtInterval) {
            await UniTask.WaitUntil(() => World.Services?.TryGet<RecurringActions>() != null);
            World.Services.Get<RecurringActions>().RegisterAction(() => RecurringEventUtil.Trigger(evtTarget, evtName), RecurringEventUtil.ID(evtTarget, evtName), evtInterval);
        }
    }
}