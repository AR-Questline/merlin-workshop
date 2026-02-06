using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Crafting.Slots {
    [UsesPrefab("Crafting/VGhostItem")]
    public class VGhostItem : View<GhostItem> {
        static readonly int Grayscale = Shader.PropertyToID("_Grayscale");

        [SerializeField] Image ghostImage;
        
        public override Transform DetermineHost() => Target.DeterminedHost;

        Material _iconMaterial;
        
        protected override void OnInitialize() {
            if (!Target.wantedItemTemplate.iconReference.IsSet) {
                Log.Important?.Error(Target.wantedItemTemplate.ItemName + "icon reference is null, please add an icon!");
                return;
            }
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            
            _iconMaterial = new Material(ghostImage.material);
            ghostImage.material = _iconMaterial;
            
            Refresh();
        }
        
        void Refresh() {
            if (Target.Item == null) {
                Target.wantedItemTemplate.iconReference.RegisterAndSetup(this, ghostImage);
            } else {
                Target.Item.Icon.RegisterAndSetup(this, ghostImage);
            }
            
            bool canCraft = Target.inventoryQuantity >= Target.requiredQuantity;
            var grayscale = canCraft ? 0 : 1;
            _iconMaterial.SetFloat(Grayscale, grayscale);
        }
    }
}
