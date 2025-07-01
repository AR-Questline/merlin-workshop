using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Crafting {
    /// <summary>
    /// Holds the currently selected slot and available slots for adding CraftingItems to CraftingSlots
    /// </summary>
    public class SelectedWorkbenchSlot {
        [UnityEngine.Scripting.Preserve] public Model parent;
        int _currentSlot = 0;

        int CurrentSlot {
            get => _currentSlot;
            set => _currentSlot = value % _interactableSlots.Count;
        }

        readonly List<WorkbenchSlot> _interactableSlots = new();

        /// <summary>
        /// Generates container for selectable slots
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="condition">what defines a slot to be selectable</param>
        /// <param name="slots">all available slots</param>
        public SelectedWorkbenchSlot(Model parent, ModelsSet<WorkbenchSlot> slots) {
            this.parent = parent;
            foreach (WorkbenchSlot ingredientSlot in slots) {
                _interactableSlots.Add(ingredientSlot);
            }
        }

        public void RemoveAllWorkbenchItems() {
            _interactableSlots.ForEach(x => x.RemoveElementsOfType<CraftingItem>());
            CurrentSlot = 0;
        }

        public WorkbenchSlot Current => _interactableSlots.Count > 0 ? _interactableSlots[CurrentSlot] : null;
        public void MoveNext() => CurrentSlot++;

        /// <summary>
        /// Set current slot to empty slot
        /// </summary>
        /// <returns>Whether an empty slot was found</returns>
        public bool GoToEmpty() {
            int result = _interactableSlots.FindIndex(x => x.CraftingItem == null);
            if (result >= 0) {
                _currentSlot = result;
                return true;
            }

            return false;
        }
    }
}