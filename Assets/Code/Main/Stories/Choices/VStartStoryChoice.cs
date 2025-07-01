using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using Awaken.TG.Utility;
using Awaken.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Choices {
    public class VStartStoryChoice : View<StartStoryChoice>, IHoverableView, IFocusSource {
        public ARButton button;
        public TextMeshProUGUI mainText;
        public TextMeshProUGUI additionalText;
        
        // === Bridge to showing additional data
        string AggregatedEffects => string.Join("\n", Effects.Where(eff => !string.IsNullOrWhiteSpace(eff)));
        IEnumerable<string> Effects {
            get {
                yield return Target.EffectAndCost;
            }
        }
        
        // Force Focus if something else than other VChoice is selected
        public bool ForceFocus => World.Only<Focus>().Focused?.GetComponentInParent<VStartStoryChoice>() == null;
        public Component DefaultFocus => button;
        
        // === View
        public override Transform DetermineHost() => Target.Story.LastChoicesGroup();
        
        // === Initialization
        protected override void OnInitialize() {
            SetValues();

            // add 0.5s delay on callback, so user doesn't click it unintentionally
            DOVirtual.DelayedCall(0.5f, () => button.OnClick += Target.Callback);
        }
        
        // === Data 
        void SetValues() {
            button.Interactable = Target.Enable;
            mainText.text = Target.ButtonText.FormatSprite();
            mainText.color = Target.Enable ? (Target.IsMainChoice ? ARColor.MainAccent : ARColor.MainWhite ) : ARColor.DarkerGrey;
            if (!string.IsNullOrWhiteSpace(AggregatedEffects)) {
                additionalText.text = AggregatedEffects;
                additionalText.color = Target.Enable ? ARColor.MainGrey : ARColor.MainRed;
            } else {
                additionalText.gameObject.SetActive(false);
            }
        }

        // === Hover
        public UIResult Handle(UIEvent evt) {
            return UIResult.Ignore;
        }
    }
}