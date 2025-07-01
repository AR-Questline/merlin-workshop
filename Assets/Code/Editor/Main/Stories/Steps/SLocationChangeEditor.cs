using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Steps;
using TargetType = Awaken.TG.Main.Locations.TargetType;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    // [CustomElementEditor(typeof(SLocationChange))]
    // public class SLocationChangeEditor : ElementEditor {
    //     protected override void OnElementGUI() {
    //         var step = Target<SLocationChange>();
    //         LocationReference locRef = step.locationReference;
    //
    //         if (locRef.targetTypes == TargetType.Tags) {
    //             DrawProperties("tags");
    //         }
    //         
    //         if (locRef.targetTypes == TargetType.Templates) {
    //             DrawProperties("locationRefs");
    //         }
    //         
    //         if (locRef.targetTypes == TargetType.DirectReferences) {
    //             DrawProperties("directReferences");
    //         }
    //         
    //         DrawPropertiesExcept("tags", "locationRefs", "directReferences");
    //     }
    // }
}