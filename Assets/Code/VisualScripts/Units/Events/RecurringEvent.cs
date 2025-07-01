using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units.Utils;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Events {
    [UnitCategory("Events")]
    [UnitOrder(2)]
    [TypeIcon(typeof(CustomEvent))]
    [UnityEngine.Scripting.Preserve]
    public class RecurringEvent : Unit, IGraphElementWithData {
        static ulong s_guid = 0;
        public ValueInput interval;
        ControlOutput _trigger;

        protected override void Definition() {
            var start = ControlInput("start", StartRecurring);
            var stop = ControlInput("stop", StopRecurring);
            _trigger = ControlOutput("Trigger");

            interval = ValueInput("Interval", 1F);

            Succession(start, _trigger);
            Requirement(interval, start);
        }

        ControlOutput StartRecurring(Flow flow) {

            float evtInterval = flow.GetValue<float>(interval);
            Data elementData = flow.stack.GetElementData<Data>(this);

            if (elementData.Reference == null) {
                elementData.guid ??= "VSRecurring: " + ++s_guid;
                elementData.Reference = flow.stack.AsReference();

                RegisterRecurringAction(elementData, evtInterval);
            }

            return null;
        }

        ControlOutput StopRecurring(Flow flow) {
            Data elementData = flow.stack.GetElementData<Data>(this);
            elementData.SetReference(null);
            UnregisterRecurringAction(elementData.guid);
            return null;
        }

        void RegisterRecurringAction(Data data, float evtInterval) {
            World.Services.Get<RecurringActions>().RegisterAction(() => Trigger(data), data.guid, evtInterval);
        }

        void UnregisterRecurringAction(string guid) {
            World.Services.Get<RecurringActions>().UnregisterAction(guid);
        }

        void Trigger(Data data) {
            SafeGraph.Run(AutoDisposableFlow.New(data.Reference), _trigger);
        }

        public IGraphElementData CreateData() {
            return new Data();
        }

        protected class Data : IGraphElementData {
            public string guid;
            GraphReference _reference;

            public GraphReference Reference {
                get => _reference;
                set {
                    _reference = value;
                }
            }

            public void SetReference(GraphReference reference) {
                this._reference = reference;
            }
        }
    }
}