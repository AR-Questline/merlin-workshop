using Awaken.Utility;
using System.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    public partial class FlowerpotSlot : FurnitureSlotBase<FlowerpotFurnitureSlotAttachment> {
        public override ushort TypeForSerialization => SavedModels.FlowerpotSlot;

        protected override void AfterVisualLoaded(Transform parentTransform, bool variantChangedByPlayer = false) {
            base.AfterVisualLoaded(parentTransform, variantChangedByPlayer);

            bool hasPlantSlotMarkers = parentTransform.GetComponentsInChildren<PlantSlotMarkerInteraction>().Any();
            if (!hasPlantSlotMarkers) {
                return;
            }

            if (TryGetElement(out Flowerpot flowerpot)) {
                flowerpot.RestorePlantSlots(parentTransform);
                return;
            }
            
            flowerpot = AddElement(new Flowerpot());
            flowerpot.InitializePlantSlots(parentTransform);
        }
    }
}