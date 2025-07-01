using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.GamepadKeyboard.Keys {
    public class Key : MonoBehaviour {
        [SerializeField] protected TMP_Text _key;
        
        public delegate void OnKeyClickedHandler(string key);

        // The event which other objects can subscribe to
        // Uses the function defined above as its type
        public event OnKeyClickedHandler OnKeyClicked;

        public virtual void Awake() {
            GetComponent<Button>().onClick.AddListener(() => {
                OnKeyClicked?.Invoke(_key.text);
            });
        }

        public virtual void CapsLock(bool isUppercase) { }
        public virtual void ShiftKey(bool isPressed) { }
    };
}