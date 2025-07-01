using System.Linq;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/VTitleScreenSceneSelection")]
    public class VTitleScreenSceneSelection : View<TitleScreenSceneSelection> {
        [SerializeField] TMP_Dropdown _scenesDropdown;
        [SerializeField] ARButton _load;
        [SerializeField] ARButton _hide;

        int _selectedOption = -1;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            _scenesDropdown.options = Target.SceneReferences.Select(s => new TMP_Dropdown.OptionData(s.Name)).ToList();
            _scenesDropdown.onValueChanged.AddListener(DropdownChanged);

            _load.OnClick += Load;
            _hide.OnClick += Hide;
            
            UpdateState();
        }
        
        void Hide() {
            Target.Hide();
        }
        
        void Load() {
            Target.Load(_selectedOption);
        }
        
        void DropdownChanged(int selectedItem) {
            _selectedOption = selectedItem;
            UpdateState();
        }
        
        void UpdateState() {
            _load.Interactable = _selectedOption > -1;
        }
    }
}