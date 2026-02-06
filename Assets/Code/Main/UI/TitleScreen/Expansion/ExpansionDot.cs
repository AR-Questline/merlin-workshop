using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    public class ExpansionDot : MonoBehaviour {
        [SerializeField] GameObject selected;

        public void Select(bool state) {
            selected.SetActiveOptimized(state);
        }
    }
}