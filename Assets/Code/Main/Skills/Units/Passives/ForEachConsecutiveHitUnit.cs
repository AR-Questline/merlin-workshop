using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Skills/Passives")]
    [UnitTitle("For Each Consecutive Hit")]
    public class ForEachConsecutiveHitUnit : PassiveUnit, IGraphElementWithData {
        
        static int s_id;
        
        ValueInput _maxDelay;
        ValueOutput _damage;
        ValueOutput _index;
        ValueOutput _counterRef;
        ControlOutput _trigger;
        ControlOutput _onDelayElapsed;
        ControlOutput _onIndexUpdated;

        protected override void Definition() {
            _maxDelay = ValueInput("Max Delay", 5.0f);
            _trigger = ControlOutput("on consecutive hit");
            _onDelayElapsed = ControlOutput("on delay elapsed");
            _onIndexUpdated = ControlOutput("on index updated");
            _damage = ValueOutput<Damage>("damage");
            _index = ValueOutput<int>("index");
            _counterRef = ValueOutput<DataVsView>("counterRef");
        }

        public override void Enable(Skill skill, Flow flow) {
            var maxDelay = flow.GetValue<float>(_maxDelay);
            var data = flow.stack.GetElementData<Data>(this);
            var pointer = flow.stack.AsReference();
            data.id = s_id++;
            data.maxDelay = maxDelay;
            data.listener = skill.Owner.ListenTo(HealthElement.Events.BeforeDamageMultiplied, dmg => BeforeDamageMultiplied(dmg, pointer), skill);
            data.reference = flow.stack.AsReference();
            
            flow.SetValue(_index, 0);
            flow.SetValue(_counterRef, data.vsView);
        }

        public override void Disable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            World.Services.Get<RecurringActions>().UnregisterAction(GetRecurringActionId(data));
            data.alive = null;
            data.consecutiveHits = 0;
            World.EventSystem.TryDisposeListener(ref data.listener);
            data.reference = null;
        }

        public IGraphElementData CreateData() {
            return new Data();
        }

        void BeforeDamageMultiplied(Damage damage, GraphPointer pointer) {
            var data = pointer.GetElementData<Data>(this);
            var time = Time.time;
            if (data.alive == damage.Target && time < data.lastHitTime + data.maxDelay) {
                data.consecutiveHits++;
                
                SafeGraph.Run(PrepareFlow(data, damage), _trigger);

                var recurringActions = World.Services.Get<RecurringActions>();
                string id = GetRecurringActionId(data);
                recurringActions.UnregisterAction(id);
                recurringActions.RegisterAction(() => OnDelayElapsed(pointer), id, data.maxDelay, false);
            } else {
                data.alive = damage.Target;
                data.consecutiveHits = 0;
            }
            data.lastHitTime = time;
            
            SafeGraph.Run(PrepareFlow(data, damage), _onIndexUpdated);
        }
        
        void OnDelayElapsed(GraphPointer pointer) {
            var data = pointer.GetElementData<Data>(this);
            World.Services.Get<RecurringActions>().UnregisterAction(GetRecurringActionId(data));
            data.alive = null;
            data.consecutiveHits = 0;
            
            SafeGraph.Run(PrepareFlow(data, null), _onDelayElapsed);
            SafeGraph.Run(PrepareFlow(data, null), _onIndexUpdated);
        }

        AutoDisposableFlow PrepareFlow(Data data, Damage damage) {
            var flow = AutoDisposableFlow.New(data.reference);
            flow.flow.SetValue(_damage, damage);
            flow.flow.SetValue(_index, data.consecutiveHits);
            flow.flow.SetValue(_counterRef, data.vsView);
            return flow;
        }
        
        string GetRecurringActionId(Data data) {
            return $"ForEachConsecutiveHit:{data.id}";
        }
        
        public class Data : IGraphElementData {
            public readonly DataVsView vsView;
            
            public int id;
            public float maxDelay;
            public IEventListener listener;
            public GraphReference reference;

            public IAlive alive;
            public int consecutiveHits;
            public float lastHitTime;

            public Data() {
                vsView = new DataVsView(this);
            }
        }
        
        public class DataVsView {
            readonly Data _data;
            
            public DataVsView(Data data) {
                _data = data;
            }

            [UnityEngine.Scripting.Preserve]
            public void ResetCounter() {
                _data.alive = null;
                _data.consecutiveHits = 0;
            }
        }
    }
}