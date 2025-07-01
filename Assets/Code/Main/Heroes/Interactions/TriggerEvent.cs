using Awaken.TG.Main.Character;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    public abstract class TriggerEvent : EventUnit<TriggerEvent.Data> {
        [Serialize, Inspectable, UnitHeaderInspectable] public TriggerVolume.Target target;

        ValueInput _source;
            
        ValueOutput _collider;
        ValueOutput _character;
            
        protected override bool register => true;
            
        protected override void Definition() {
            base.Definition();
            _source = ValueInput<GameObject>("source", null).NullMeansSelf();
            _collider = ValueOutput<Collider>("collider");
            _character = ValueOutput<ICharacter>("character");
        }

        GameObject Source(GraphReference reference) => Flow.FetchValue<GameObject>(_source, reference) ?? reference.gameObject;
        public override EventHook GetHook(GraphReference reference) => new(EventHook(EvtName, target), Source(reference));

        protected override void AssignArguments(Flow flow, Data args) {
            flow.SetValue(_collider, args.collider);
            flow.SetValue(_character, VGUtils.GetModel<ICharacter>(args.collider.gameObject));
        }

        protected abstract string EvtName { get; }

        public static void Trigger(GameObject source, string name, TriggerVolume.Target target, Collider collider) {
            EventBus.Trigger(EventHook(name, target), source, new Data(collider));
        }
        protected static string EventHook(string name, TriggerVolume.Target target) {
            return $"{name}_{target}";
        }

        public new struct Data {
            public Collider collider;

            public Data(Collider collider) {
                this.collider = collider;
            }
        }
    }
    

    public class Enter : TriggerEvent {
        public const string Name = "OnTriggerEnter";
        protected override string EvtName => Name;
    }
    public class Exit : TriggerEvent { 
        public const string Name = "OnTriggerExit";
        protected override string EvtName => Name;
    }
    public class Stay : TriggerEvent { 
        public const string Name = "OnTriggerStay";
        protected override string EvtName => Name;
    }
}