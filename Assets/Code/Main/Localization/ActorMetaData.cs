using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Awaken.TG.Main.Localization {
    [Metadata]
    [Serializable]
    public class ActorMetaData : IMetadata {
        [SerializeField] [TextArea(1, int.MaxValue)]
        string actorName = "";

        public ActorMetaData() {
            ActorName = string.Empty;
        }

        public ActorMetaData(string value = "") {
            ActorName = value;
        }
        
        public static implicit operator string(ActorMetaData s) {
            return s?.ActorName ?? string.Empty;
        }

        /// <summary>
        /// The comment text.
        /// </summary>
        public string ActorName {
            get => actorName;
            set => actorName = value;
        }
    }
}