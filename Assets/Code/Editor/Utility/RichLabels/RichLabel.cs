using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels {
    [Serializable]
    public class RichLabel {
        [SerializeField, HideLabel] string name;
        [SerializeField, HideInInspector] string guid;

        public string Name => name;
        public string Guid => guid;

        public RichLabel() {
            name = "New Label";
            guid = System.Guid.NewGuid().ToString();
        }
        
        public RichLabel(string name) {
            this.name = name;
            guid = System.Guid.NewGuid().ToString();
        }

        public RichLabel(string name, string guid) {
            this.name = name;
            this.guid = guid;
        }
    }
}