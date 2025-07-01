using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Main.AI.Barks {
    [Serializable]
    public class BarkBookmark : ScriptableObject {
        public List<BarkTextCollection> barkTextCollections = new();
    }

    [Serializable]
    public class BarkTextCollection {
        public string tag;
        public List<string> phrases = new();
    }
}