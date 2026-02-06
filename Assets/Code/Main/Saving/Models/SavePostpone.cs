using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Saving.Models {
    /// <summary>
    /// Postpones save, useful for asynchronous logic that needs to be completed before save happens 
    /// </summary>
    public partial class SavePostpone : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        static readonly HashSet<SaveSlot> Slots = new();
        static readonly Dictionary<SaveSlot, bool> SlotDeleteIfSavingFailed = new();

        readonly Flow _flow;

        bool IsValid => _flow.stack.isValid;
        
        // For debug purposes
        readonly string _sourceGameObjectName;
        
        // === Conctructor
        
        SavePostpone(Flow flow) {
            _flow = flow;
            if (flow.stack.gameObject) {
                _sourceGameObjectName = flow.stack.gameObject.name;
            } else {
                _sourceGameObjectName = "No game object attached to Flow";
            }
        }

        // === Static helpers
        public static bool AnySavePostponed() {
            foreach (var postpone in World.All<SavePostpone>()) {
                if (!postpone.IsValid) {
                    postpone.DiscardNextFrame().Forget();
                    continue;
                }
                return true;
            }
            return false;
        }
        
        public static bool ShouldPostpone(SaveSlot slot, bool deleteIfSavingFailed) {
            SavePostpone anyPostpone = World.All<SavePostpone>().FirstOrDefault(sp => sp is { IsBeingDiscarded: false, IsValid: true });
            if (anyPostpone != null) {
                Slots.Add(slot);
                SlotDeleteIfSavingFailed[slot] = deleteIfSavingFailed;
                Log.Marking?.Warning($"Saving in slot {slot?.ID} blocked by {anyPostpone._sourceGameObjectName}");
                return true;
            }

            return false;
        }

        public static SavePostpone Create(Flow flow) {
            if (World.HasAny<Hero>()) {
                var postpone = World.Add(new SavePostpone(flow));
                return postpone;
            }

            return null;
        }

        // === Discarding - auto saving
        async UniTaskVoid DiscardNextFrame() {
            if (await AsyncUtil.DelayFrame(this)) {
                Discard();
            }
        }
        
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
                    try {
                        LoadSave.Get.Save(slot, SlotDeleteIfSavingFailed[slot]);
                    } catch {
                        Log.Critical?.Error($"Failed to save in slot {slot?.ID}");
                    }
                }
            } else {
                // We can't save and there are no other postpones waiting, so we fail
                Log.Important?.Error($"Failed to save from postpone, slots: {string.Join(", ", Slots.Select(s => s.DisplayName))}");
                SaveLoadUnavailableInfo.ShowSaveUnavailableInfo();
            }

            SlotDeleteIfSavingFailed.Clear();
        }
    }
}