using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Localization {
    [Serializable]
    public partial struct OptionalLocString {
        public ushort TypeForSerialization => SavedTypes.OptionalLocString;

        [Saved] public bool toggled;
        [Saved, SerializeField] LocString locString;

        public LocString LocString => toggled ? locString : new();
        
        public OptionalLocString(LocString locString, bool toggled) {
            this.locString = locString;
            this.toggled = toggled;
        }
    }
}