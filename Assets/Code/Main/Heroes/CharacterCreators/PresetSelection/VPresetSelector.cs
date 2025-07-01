using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection {
    [UsesPrefab("CharacterCreator/PresetSelection/" + nameof(VPresetSelector))]
    public class VPresetSelector : View<PresetSelector>, IAutoFocusBase {
        [SerializeField] Transform promptHost;
        [SerializeField] TMP_Text title;
        [SerializeField] Transform presetButtonHost;
        [SerializeField] Scrollbar scroll;
        [SerializeField] ARButton incrementButton;
        [SerializeField] ARButton decrementButton;
        [SerializeField] float step = 1f;

        public Transform PresetButtonHost => presetButtonHost;
        public Transform PromptsHost => promptHost;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnFullyInitialized() {
            title.text = LocTerms.CharacterCreatorGameplayPresets.Translate();
            incrementButton.OnClick += Increment;
            decrementButton.OnClick += Decrement;
        }

        protected override void OnMount() {
            AttachToScroll().Forget();
        }

       async UniTaskVoid AttachToScroll() {
            await AsyncUtil.DelayFrame(Target);
            scroll.onValueChanged.AddListener(SetupButtons);
            SetupButtons(scroll.value);
        }

        void Increment() {
            scroll.value = Mathf.Clamp(scroll.value + step, 0, 1);
        }
        
        void Decrement() {
            scroll.value = Mathf.Clamp(scroll.value - step, 0, 1);
        }

        void SetupButtons(float scrollValue) {
            incrementButton.gameObject.SetActiveOptimized(scrollValue < 0.99f);
            decrementButton.gameObject.SetActiveOptimized(scrollValue > 0.01f);
        }
    }
}