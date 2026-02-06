using System;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items {
    /// <summary>
    /// Marker script for locking item. It prevents changing item slot and also prevents dropping item.
    /// </summary>
    public partial class LockItemSlot : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.LockItemSlot;

        [Saved] bool _allowPerspectiveChangeReequip;
        [Saved] LockSource _lockSource = LockSource.CutOffHand;

        public bool AllowPerspectiveChangeReequip => _allowPerspectiveChangeReequip;
        public LockSource Source => _lockSource;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public LockItemSlot() { }
        
        public LockItemSlot(bool allowPerspectiveChangeReequip, LockSource source) {
            _allowPerspectiveChangeReequip = allowPerspectiveChangeReequip;
            _lockSource = source;
        }

        [Serializable]
        public enum LockSource {
            CutOffHand,
            Story,
            TemporaryItem,
            TemporaryItemSource
        }
    }
}