using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Locations.Containers {
    [UsesPrefab("Locations/" + nameof(VContainerUI))]
    public class VContainerUI : View<ContainerUI>, IVisualElementPromptHost {
        public VisualElement VisualPromptHost { get; set; }
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
    }
}