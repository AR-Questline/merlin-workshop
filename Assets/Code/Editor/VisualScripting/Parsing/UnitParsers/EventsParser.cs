using System.Linq;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility;
using Awaken.TG.VisualScripts.Units.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class EventsParser {
        public static void TriggerCustomEvent(TriggerCustomEvent trigger, FunctionScript script) {
            script.AddFlow($"CustomEvent.Trigger({script.Variable(trigger.target)}, {script.Variable(trigger.name)}{FunctionMaker.AdditionalArguments(trigger.arguments.Select(script.Variable))});");
        }

        public static void RegisterRecurringEvent(RegisterRecurringEvent evt, FunctionScript script) {
            script.AddUsing("Awaken.TG.Main.Timing");
            script.AddUsing("Awaken.TG.Main.Utility");
            script.AddUsing("Awaken.TG.VisualScripts.Units.Events");
            script.AddUsing("Cysharp.Threading.Tasks");
            script.AddAsync();
            script.AddFlow("await UniTask.WaitUntil(() => SceneGlobals.Services.TryGet<RecurringActions>() != null);");
            script.AddFlow($"SceneGlobals.Services.Get<RecurringActions>().RegisterAction(() => RecurringEventUtil.Trigger(gameObject, {script.Variable(evt.name)}), RecurringEventUtil.ID(gameObject, {script.Variable(evt.name)}), {script.Variable(evt.interval)});");
        }
        public static void UnregisterRecurringEvent(UnregisterRecurringEvent evt, FunctionScript script) {
            script.AddUsing("Awaken.TG.Main.Timing");
            script.AddUsing("Awaken.TG.Main.Utility");
            script.AddUsing("Awaken.TG.VisualScripts.Units.Events");
            script.AddFlow($"SceneGlobals.Services.Get<RecurringActions>().UnregisterAction(RecurringEventUtil.ID(gameObject, {script.Variable(evt.name)}));");
        }
    }
}