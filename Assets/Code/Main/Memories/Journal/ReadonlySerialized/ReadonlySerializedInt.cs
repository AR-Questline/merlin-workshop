using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.ReadonlySerialized {
    [Serializable]
    public struct SerializedReadonlyInt {
        [SerializeField, HideLabel] int value;
        public int Value => value;
        
        public SerializedReadonlyInt(int value) {
            this.value = value;
        }
        
        public override string ToString() => value.ToString();
    }
}