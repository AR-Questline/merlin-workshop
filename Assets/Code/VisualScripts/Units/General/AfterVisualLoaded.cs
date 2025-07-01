using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class AfterVisualLoaded : ARUnit {
        protected override void Definition() {
            var inLocation = FallbackARValueInput("location", VGUtils.My<Location>);
            
            var outLocation = ValueOutput<Location>("location");
            var outVisual = ValueOutput<Transform>("visual");

            var output = ControlOutput("");
            var input = ControlInput("", flow => {
                var postpone = SavePostpone.Create(flow);
                
                var reference = flow.stack.AsReference();
                var location = inLocation.Value(flow);
                
                location.OnVisualLoaded(transform => {
                    var f = AutoDisposableFlow.New(reference);
                    
                    f.flow.SetValue(outLocation, location);
                    f.flow.SetValue(outVisual, transform);
                    
                    SafeGraph.Run(f, output);
                    
                    postpone.Discard();
                }, true);
                
                return null;
            });
            
            Succession(input, output);
        }
    }
}