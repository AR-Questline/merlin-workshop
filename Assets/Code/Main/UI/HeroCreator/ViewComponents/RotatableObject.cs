using UnityEngine;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    /// <summary>
    /// Marker for GameObject that should be rotated by VCRotator
    /// </summary>
    public class RotatableObject : MonoBehaviour {
        public bool Rotatable { get; private set; } = true;
        
        public void SetCanRotate(bool canRotate) {
            Rotatable = canRotate;
        }
    }
}