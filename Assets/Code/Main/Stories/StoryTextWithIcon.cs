using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories {
    public class StoryTextWithIcon : MonoBehaviour {
        [UnityEngine.Scripting.Preserve] public Image icon;
        [UnityEngine.Scripting.Preserve] public TextMeshProUGUI text;
        [UnityEngine.Scripting.Preserve] public SpriteAtlas buttonIcons;
    }
}
