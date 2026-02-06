using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Typing {
    public class RuntimeRef<T> where T : class {
        [DoNotSerialize] public T Reference { get; init; }
    }
    
    [UnitCategory("AR/RuntimeRef/Create")]
    [TypeIcon(typeof(FlowGraph))]
    public class CreateRuntimeRef<TRef, T> : ARUnit where TRef : RuntimeRef<T>, new() where T : class {
        protected override void Definition() {
            var input = ValueInput<T>("ref");
            ValueOutput(typeof(T).Name, flow => new TRef {
                Reference = flow.GetValue<T>(input),
            });
        }
    }
    
    [UnitCategory("AR/RuntimeRef/TryGet")]
    [TypeIcon(typeof(FlowGraph))]
    public class TryGetRuntimeRef<TRef, T> : ARUnit where TRef : RuntimeRef<T>, new() where T : class {
        protected override void Definition() {
            var inRef = ValueInput<TRef>("ref?");
            var outRef = ControlOutput("ref");
            var outNull = ControlOutput("null");
            var outValue = ValueOutput<T>("value");
            var input = ControlInput("in", flow => {
                var reference = flow.GetValue<TRef>(inRef)?.Reference;
                if (reference != null) {
                    flow.SetValue(outValue, reference);
                    return outRef;
                } else {
                    flow.SetValue(outValue, null);
                    return outNull;
                }
            });
            
            Succession(input, outRef);
            Succession(input, outNull);
        }
    }
    
    [UnitCategory("AR/RuntimeRef/Get")]
    [TypeIcon(typeof(FlowGraph))]
    public class GetRuntimeRef<TRef, T> : ARUnit where TRef : RuntimeRef<T>, new() where T : class {
        protected override void Definition() {
            var inRef = ValueInput<TRef>("value?");
            var inFallback = ValueInput<T>("fallback");
            ValueOutput<T>("value", flow => flow.GetValue<TRef>(inRef)?.Reference ?? flow.GetValue<T>(inFallback));
        }
    }
    
    // Toggleable Kill Prevention Element
    
    [UnityEngine.Scripting.Preserve]
    public class ToggleableKillPreventionElementRuntimeRef : RuntimeRef<ToggleableKillPreventionElement> { }
    [UnityEngine.Scripting.Preserve]
    public class CreateToggleableKillPreventionElementRuntimeRef : CreateRuntimeRef<ToggleableKillPreventionElementRuntimeRef, ToggleableKillPreventionElement> { }
    [UnityEngine.Scripting.Preserve]
    public class TryGetToggleableKillPreventionElementRuntimeRef : TryGetRuntimeRef<ToggleableKillPreventionElementRuntimeRef, ToggleableKillPreventionElement> { }
    [UnityEngine.Scripting.Preserve]
    public class GetToggleableKillPreventionElementRuntimeRef : GetRuntimeRef<ToggleableKillPreventionElementRuntimeRef, ToggleableKillPreventionElement> { }
    
    // Location
    
    [UnityEngine.Scripting.Preserve]
    public class LocationRuntimeRef : RuntimeRef<Location> { }
    [UnityEngine.Scripting.Preserve]
    public class CreateLocationRuntimeRef : CreateRuntimeRef<LocationRuntimeRef, Location> { }
    [UnityEngine.Scripting.Preserve]
    public class TryGetLocationRuntimeRef : TryGetRuntimeRef<LocationRuntimeRef, Location> { }
    [UnityEngine.Scripting.Preserve]
    public class GetLocationRuntimeRef : GetRuntimeRef<LocationRuntimeRef, Location> { }
}