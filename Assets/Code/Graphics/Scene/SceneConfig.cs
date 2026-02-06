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
        [TableColumnWidth(70, false)]
        public bool allowWyrdNight;
        [TableColumnWidth(50, false)]
        public bool additive;
        [TableColumnWidth(50, false)]
        public bool prologue;

        public SceneConfig Clone() {
            return this.MemberwiseClone() as SceneConfig;
        }
        
        public bool IsSarrasDlcScene => (openWorld && sceneName.Equals("CampaignMap_Sarras")) 
                                        || directory.Contains("Sarras", StringComparison.OrdinalIgnoreCase);
    }

    public readonly struct SceneData {
        public readonly bool openWorld;
        public readonly bool allowWyrdNight;
        public readonly bool additive;
        public readonly bool prologue;

        public SceneData(SceneConfig config) {
            openWorld = config.openWorld;
            allowWyrdNight = config.allowWyrdNight;
            additive = config.additive;
            prologue = config.prologue;
        }
    }
}
