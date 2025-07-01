using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories {
    public class StoryFancyPanel : MonoBehaviour {
        [UnityEngine.Scripting.Preserve] public Image background;
        [UnityEngine.Scripting.Preserve] public TextMeshProUGUI textWithStats;
        [UnityEngine.Scripting.Preserve] public TextMeshProUGUI textMiddle;
        [UnityEngine.Scripting.Preserve] public TextMeshProUGUI textStats;
        [UnityEngine.Scripting.Preserve] public TextMeshProUGUI textAdditional;
        [UnityEngine.Scripting.Preserve] public SpriteAtlas icons;
        [UnityEngine.Scripting.Preserve] public GameObject contentWithStats;
        [UnityEngine.Scripting.Preserve] public GameObject contentTextOnly;
        [UnityEngine.Scripting.Preserve] public FancyPanelType FancyPanelType { get; set; }
    }
}
