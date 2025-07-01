using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI {
    public class TextInputCharCounter : MonoBehaviour {

        TextMeshProUGUI _textCounter;
        TMP_InputField _inputField;
        bool ProperlySet => _inputField != null && _textCounter != null;
    
        void Awake() {
            _inputField = transform.parent.GetComponent<TMP_InputField>();
            _textCounter = transform.GetComponent<TextMeshProUGUI>();
        }

        void OnEnable() {
            if (!ProperlySet) {
                gameObject.SetActive(false);
            }
        }

        void Update() {
            if (ProperlySet) {
                _textCounter.text = $"{_inputField.text.Length.ToString()}/{_inputField.characterLimit.ToString()}";
            }
        }
    }
}
