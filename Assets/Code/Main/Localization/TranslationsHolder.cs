using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Main.Localization {
    public class TranslationsHolder : MonoBehaviour {
        [UnityEngine.Scripting.Preserve] public List<LocString> translations = new List<LocString>();
    }
}
