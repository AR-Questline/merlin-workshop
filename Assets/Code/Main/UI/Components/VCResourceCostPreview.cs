using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class VCResourceCostPreview : ViewComponent<IWithResourceCostPreview> {
        [SerializeField] Image itemIcon;
        [SerializeField] TextMeshProUGUI quantityText;
        [SerializeField] GameObject resourceGroup;
        
        SpriteReference _iconReference;
        
        static Color CanAffordColor => ARColor.MainWhite;
        static Color CantAffordColor => ARColor.MainGrey;
        
        protected override void OnAttach() {
            Target.ListenTo(IWithResourceCostPreview.Events.ResourceCostRefreshed, Refresh, this);
        }

        void Refresh(bool visible) {
            if (!visible) {
                resourceGroup.SetActive(false);
                return;
            }
            
            Color color = Target.HasRequiredQuantity ? CanAffordColor : CantAffordColor;
            string quantityFormatted =
                $"{Target.CurrentQuantity.ToString().ColoredText(color)}/{Target.RequiredQuantity.ToString().Bold()}";
            quantityText.SetText(quantityFormatted);

            if (Target.ItemTemplate.IconReference is {IsSet: true}) {
                _iconReference?.Release();
                _iconReference = Target.ItemTemplate.IconReference.Get();
                _iconReference.SetSprite(itemIcon);
                itemIcon.color = color;
            }
            
            resourceGroup.SetActive(true);
        }

        protected override void OnDiscard() {
            _iconReference?.Release();
            _iconReference = null;
        }
    }
}