using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class ColoredItem : MonoBehaviour {
        [RichEnumExtends(typeof(ARColor))][RichEnumSearchBox]
        public RichEnumReference colorReference;
        
        ARColor Color => colorReference.EnumAs<ARColor>();

        [Button(Name = "Do Color")]
        void Awake() {
            PerformColoring();
        }

        [ContextMenu("Perform")]
        void PerformColoring() {
            var tmp = GetComponent<TextMeshProUGUI>();
            if (tmp != null) {
                tmp.color = Color.Color;
            }

            var img = GetComponent<Image>();
            if (img != null) {
                img.color = Color.Color;
            }
        }
    }
}