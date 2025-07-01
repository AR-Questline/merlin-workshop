using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Debugging.DebugSetupComponents {
    public class DebugSetFlag : MonoBehaviour, IDebugComponent {
        public TextMeshProUGUI textComponent;
        public Toggle toggle;

        CEditorFlag _cEditorFlag;

        public void Init(Story story, CEditorFlag element) {
            _cEditorFlag = element;
            textComponent.text = element.flag;
            toggle.isOn = StoryFlags.Get(element.flag);
        }

        public void Apply(Story story) {
            StoryFlags.Set(_cEditorFlag.flag, toggle.isOn);
        }
    }
}