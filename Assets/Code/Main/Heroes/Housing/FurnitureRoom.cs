using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing {
    public partial class FurnitureRoom : Element<Location>, IRefreshedByAttachment<FurnitureRoomAttachment> {
        public override ushort TypeForSerialization => SavedModels.FurnitureRoom;

        [Saved] bool _isUnlocked;

        Transform _barrierTransform;

        public HousingUnlockRequirement UnlockRequirement { get; private set; }
        public bool CanUnlock => HousingUtils.HasRequiredFunds(UnlockRequirement) && HousingUtils.HasRequiredResources(UnlockRequirement);
        
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public ShareableSpriteReference RoomIcon { get; private set; }
        FurnitureRoomAttachment RoomAttachment { get; set; }
        
        public bool IsUnlocked => _isUnlocked;
        public IEnumerable<FurnitureSlotBase> Slots => World.All<FurnitureSlotBase>().Where(s => s.FurnitureRoomAttachment == RoomAttachment);

        public void InitFromAttachment(FurnitureRoomAttachment spec, bool isRestored) {
            DisplayName = spec.displayName;
            Description = spec.description;
            RoomIcon = spec.roomIcon;
            UnlockRequirement = spec.unlockRequirement;
            _barrierTransform = spec.barrierTransform;
            RoomAttachment = spec;
        }

        protected override void OnInitialize() {
            TryToUnlock();
        }

        void TryToUnlock() {
            _isUnlocked = UnlockRequirement.unlockPrice <= 0 && UnlockRequirement.requiredResources.IsNullOrEmpty();
            if (_isUnlocked) {
                DisableBarrier();
            }
        }

        void DisableBarrier() {
            if (_barrierTransform) {
                _barrierTransform.gameObject.SetActive(false);
            }
        }

        protected override void OnRestore() {
            if (_barrierTransform) {
                _barrierTransform.gameObject.SetActive(!_isUnlocked);
            }
        }

        public void UnlockRoom() {
            if (CanUnlock && !_isUnlocked) {
                _isUnlocked = true;
                HousingUtils.UseRequiredResources(UnlockRequirement);
                DisableBarrier();
            }
        }
    }
}