using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    public partial class Flowerpot : Element<IModel> {
        public override ushort TypeForSerialization => SavedModels.Flowerpot;

        public ModelsSet<PlantSlot> PlantSlots => Elements<PlantSlot>();

        protected override void OnInitialize() {
            // if Parent Model is Location it means it is self-sustained flowerpot standing somewhere in the world
            if (ParentModel is Location location) {
                location.OnVisualLoaded(AfterVisualLoaded);
            }
        }

        public bool IsInteractable() {
            if (ParentModel is Location location) {
                return location.Interactability.interactable;
            }

            // if Parent Model is not Location it means it is a flowerpot in the player's house so it should be interactable
            return true;
        }

        public void InitializePlantSlots(Transform parentTransform) {
            List<PlantSlot> childSlots = new();
            PlantSlotMarkerInteraction parentMarker = parentTransform.GetComponentsInChildren<PlantSlotMarkerInteraction>().FirstOrDefault(parentMarker => parentMarker.parent == null);
            if (parentMarker == null) {
                Log.Important?.Error("Parent Plant Slot Marker not found! This should not happen!");
                return;
            }
            
            PlantSlot parentSlot = new PlantSlot(parentMarker);
            AddElement(parentSlot);
            parentMarker.AssignPlantSlot(parentSlot);
            
            foreach (PlantSlotMarkerInteraction childMarker in parentMarker.children) {
                if (childMarker == null) {
                    Log.Important?.Error("Child Plant Slot Marker not assigned! Check Plant Slot Markers in the flowerpot location prefab!");
                    continue;
                }
                
                PlantSlot childSlot = new PlantSlot(childMarker);
                AddElement(childSlot);
                childSlots.Add(childSlot);
                childSlot.BuildStructure(parentSlot, null);
                childMarker.AssignPlantSlot(childSlot);
            }
            
            parentSlot.BuildStructure(null, childSlots.ToArray());
        }

        public void RestorePlantSlots(Transform parentTransform) {
            PlantSlotMarkerInteraction parentMarker = parentTransform.GetComponentsInChildren<PlantSlotMarkerInteraction>().FirstOrDefault(parentMarker => parentMarker.parent == null);
            if (parentMarker == null) {
                Log.Important?.Error("Parent Plant Slot Marker not found! This should not happen!"); 
                return;
            }

            PlantSlot parentSlot = Elements<PlantSlot>().First(p => p.parent == null);
            parentSlot.UpdatePlantSlot(parentMarker);
            parentMarker.AssignPlantSlot(parentSlot);
                
            var plantSlots = Elements<PlantSlot>().Where(p => p.parent != null && p.children == null).ToArray();
            if (parentMarker.children.Length != plantSlots.Length) {
                Log.Important?.Error("Child Plant Slot Markers count does not match Child Plant Slots count! This should not happen!");
                return;
            }
                
            for (int i = 0; i < plantSlots.Length; i++) {
                PlantSlotMarkerInteraction marker = parentMarker.children[i];
                PlantSlot slot = plantSlots[i];
                slot.UpdatePlantSlot(marker);
                marker.AssignPlantSlot(slot);
            }
        }

        protected void AfterVisualLoaded(Transform parentTransform) {
            if (HasElement<PlantSlot>()) {
                RestorePlantSlots(parentTransform);
                return;
            }
            
            InitializePlantSlots(parentTransform);
        }
    }
}