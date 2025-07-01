using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickItem : VCQuickItemBase {
        [Space(10f)]
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference itemTemplate;
        
        public ItemTemplate ItemTemplate => itemTemplate.Get<ItemTemplate>();

        protected override Item RetrieveItem() {
            return HeroItems.Items
                .OrderByDescending(item => item.Quality.Priority)
                .FirstOrDefault(item => item.Template == ItemTemplate);
        }

        public override void UseItemAction() {
            if (_item is not { HasBeenDiscarded: false }) {
                FMODManager.PlayOneShot(_selectNegativeSound);
                return;
            }
            
            _item.Use();
            RadialMenu.Close();
        }
    }
}