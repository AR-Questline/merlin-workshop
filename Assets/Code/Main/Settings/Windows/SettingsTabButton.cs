using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Windows {
    public class SettingsTabButton : MonoBehaviour, ITab {
        [RichEnumExtends(typeof(SettingsTabType))]
        [SerializeField] RichEnumReference tabType;
        
        [SerializeField] Button button;
        [SerializeField] TMP_Text selectedText;
        
        Color _normalTextColor;
        Color _selectedTextColor;
        float _animationTime;

        public bool IsActive { get; private set; }
        public Button.ButtonClickedEvent OnClick => button.onClick;
        public SettingsTabType Type => tabType.EnumAs<SettingsTabType>();
        public TextMeshProUGUI TextMesh => (TextMeshProUGUI)selectedText;

        public void Select() => OnClick?.Invoke();

        public void Init(SettingsTabType tabType) {
            this.tabType = tabType;
        }
        
        public void Setup(Color normalTextColor, Color selectedTextColor, float animationTime) {
            selectedText.text = Type.DisplayName;
            
            this._normalTextColor = normalTextColor;
            this._selectedTextColor = selectedTextColor;
            this._animationTime = animationTime;
            
            Deactivate(true);
        }

        public static void SwitchTab(SettingsTabButton from, SettingsTabButton to) {
            to.Activate();
            
            if (from == null) {
                return;
            }
            from.Deactivate(false);
        }
        
        void Activate() {
            IsActive = true;
            selectedText.DOKill();
            selectedText.DOColor(_selectedTextColor, _animationTime).SetUpdate(true);
        }

        void Deactivate(bool instant) {
            IsActive = false;
            selectedText.DOKill();
            selectedText.DOColor(_normalTextColor, _animationTime).SetUpdate(true).SetInstant(instant);
        }
    }
}
