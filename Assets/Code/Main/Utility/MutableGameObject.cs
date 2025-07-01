using Awaken.TG.Main.Utility.Availability;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility {
    public class MutableGameObject : MonoBehaviour {
        [SerializeField, InlineProperty, HideLabel] 
        MutableAvailability availability;
        
        void Awake() {
            availability.Init(Show, Hide, gameObject.activeSelf);
            availability.Enable();
        }

        void OnDestroy() {
            availability.Deinit();
        }

        void Show() {
            gameObject.SetActive(true);
        }

        void Hide() {
            gameObject.SetActive(false);
        }
    }
}