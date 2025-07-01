using System;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units {
    public abstract class ARValueInput<T> {
        readonly ValueInput _value;

        protected ARValueInput(ValueInput value) {
            _value = value;
        }

        public ValueInput Port => _value;
        public bool HasValidConnection => _value.hasValidConnection;
        public string Key => _value.key;

        public T Value(Flow flow) {
            return HasValue ? flow.GetValue<T>(_value) : Fallback(flow);
        }

        public T Value(Flow flow, Func<T> fallback) {
            return HasValue ? flow.GetValue<T>(_value) : fallback();
        }

        public TModel ModelValue<TModel>(Flow flow) where TModel : class, IModel {
            T value = Value(flow);
            return value != null ? value.Convert<TModel>() : null;
        }

        public abstract bool HasValue { get; }
        protected abstract T Fallback(Flow flow);
    }
    
    public class InlineValueInput<T> : ARValueInput<T> {
        public InlineValueInput(ValueInput value) : base(value) { }
        public override bool HasValue => true;
        protected override T Fallback(Flow flow) => throw new NotImplementedException();
    }

    public class FallbackValueInput<T> : ARValueInput<T> {
        readonly Func<Flow, T> _fallback;

        public FallbackValueInput(ValueInput value, Func<Flow, T> fallback) : base(value) {
            _fallback = fallback;
        }

        public override bool HasValue => HasValidConnection;
        protected override T Fallback(Flow flow) => _fallback(flow);
    }

    public class OptionalValueInput<T> : ARValueInput<T> {
        public OptionalValueInput(ValueInput value) : base(value) { }
        public override bool HasValue => HasValidConnection;
        protected override T Fallback(Flow flow) => throw new Exception($"{Key} port is optional. It must be programmatically handled when port has no connection");
    }
    
    public class RequiredValueInput<T> : ARValueInput<T> {
        public RequiredValueInput(ValueInput value) : base(value) { }
        public override bool HasValue => HasValidConnection;
        protected override T Fallback(Flow flow) => throw new Exception($"{Key} port is required");
    }
}