using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Saving.Models {
    /// <summary>
    /// Postpones save, useful for asynchronous logic that needs to be completed before save happens 
    /// </summary>
    public partial class SavePostpone : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        static readonly HashSet<SaveSlot> Slots = new();

        // For debug purposes
        string _sourceGameObjectName;

        // === Static helpers
        public static bool ShouldPostpone(SaveSlot slot) {
            SavePostpone anyPostpone = World.All<SavePostpone>().FirstOrDefault(sp => !sp?.IsBeingDiscarded ?? false);
            if (anyPostpone != null) {
                Slots.Add(slot);
                Log.Marking?.Warning($"Saving in slot {slot?.ID} blocked by {anyPostpone._sourceGameObjectName}");
                return true;
            }

            return false;
        }

        public static SavePostpone Create(Flow flow) {
            if (World.HasAny<Hero>()) {
                var postpone = World.Add(new SavePostpone());
                postpone._sourceGameObjectName = flow.stack.gameObject != null ? flow.stack.gameObject.name : "No game object attached to Flow";
                return postpone;
            }

            return null;
        }

        // === Discarding - auto saving
        protected override void OnDiscard(bool fromDomainDrop) {
            if (!Slots.Any()) return;

            var otherPostpones = World.All<SavePostpone>().Where(sp => sp != this && (!sp?.IsBeingDiscarded ?? false));
            if (!otherPostpones.Any()) {
                // No other postpones, we can save now
                TrySave();
            }
        }

        static void TrySave() {
            List<SaveSlot> toSave = Slots.Where(s => !s.HasBeenDiscarded).ToList();
            Slots.Clear();
            
            if (LoadSave.Get.CanSystemSave()) {
                foreach (var slot in toSave) {
                    LoadSave.Get.Save(slot);
                }
            } else {
                // We can't save and there are no other postpones waiting, so we fail
                Log.Important?.Error($"Failed to save from postpone, slots: {string.Join(", ", Slots.Select(s => s.DisplayName))}");
                SaveLoadUnavailableInfo.ShowSaveUnavailableInfo();
            }
        }
    }
}