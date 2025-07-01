using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public class VCCGridSelect : View<CCGridSelect>, IVCCFocusablePart {
        [SerializeField] RectTransform content;
        [SerializeField] GridLayoutGroup grid;
        
        public Transform Content => content;

        protected override void OnInitialize() {
            if (Target.Data.Type == GridSelectType.Icon) {
                grid.cellSize = new Vector2(96, 128);
                grid.spacing = new Vector2(35, 30);
                Target.ColumnCount = 6;
            } else if (Target.Data.Type == GridSelectType.Color) {
                grid.cellSize = new Vector2(50, 50);
                grid.spacing = new Vector2(20, 20);
                Target.ColumnCount = 11;
            }
        }

        public void ReceiveFocusFromTop(float horizontalPercent) {
            FocusSelectedOption();
        }

        public void ReceiveFocusFromBottom(float horizontalPercent) {
            FocusSelectedOption();
        }

        void FocusSelectedOption() {
            var options = Target.AvailableOptions;
            int index = Mathf.Clamp(Target.SavedValue, 0, options.Length - 1);
            World.Only<Focus>().Select(options[index].MainView);
        }
    }
}