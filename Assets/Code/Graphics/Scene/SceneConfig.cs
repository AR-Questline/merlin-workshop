using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Scene {
    [Serializable, InlineEditor]
    public class SceneConfig {
        [DisplayAsString]
        public string sceneName;
        [DisplayAsString]
        public string directory;
        [DisplayAsString]
        public string GUID = "";
        [TableColumnWidth(40, false), HideInInspector] // not used currently
        public bool bake;
        [TableColumnWidth(35, false), HideInInspector] // not used currently
        public bool APV;
        [TableColumnWidth(70, false)]
        public bool openWorld;
        [TableColumnWidth(50, false)]
        public bool additive;
        [TableColumnWidth(50, false)]
        public bool prologue;

        public SceneConfig Clone() {
            return this.MemberwiseClone() as SceneConfig;
        }
    }
}
