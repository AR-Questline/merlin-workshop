using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility;
using Sirenix.Utilities;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors {
    public class ExistingItemDescriptor : IItemDescriptor {
        protected Item Item { get; }

        public ExistingItemDescriptor(Item item) {
            Item = item;
        }

        public ItemQuality Quality => Item.Quality;
        public int Quantity => Item.Quantity;
        public string Name => Item.DisplayName;

        public string ItemType => ItemUtils.ItemTypeTranslation(Item);
        public IItemTypeSpecificDescriptor TypeSpecificDescriptor => IItemTypeSpecificDescriptor.ItemTypeSpecificDescriptor(Item);

        public SpriteReference Icon => Item.Template.IconReference.Get();

        public string ItemFlavor => Item.Flavor;
        public string ItemDescription => Item.DescriptionFor(Hero.Current);
        public string ItemRequirements => Item.RequirementsDescriptionFor(Hero.Current);
        public IEnumerable<string> Effects => Item.EffectsForDescription.Select(skill => skill.DescriptionFor(Hero.Current))
            .Where(description => !description.IsNullOrWhitespace());
        
        public IEnumerable<string> GemsSlot {
            get {
                int emptyGemSlots = Item.FreeGemSlots;
                for (int i = 0; i < emptyGemSlots; i++) {
                    yield return LocTerms.UIItemsEmptyGemSlot.Translate();
                }
            }
        }

        public ItemRead Read => Item.TryGetElement<ItemRead>();
        public IEnumerable<GemAttached> Gems => Item.Elements<GemAttached>().GetManagedEnumerator();
        public IEnumerable<AppliedItemBuff> Buffs => Item.Elements<AppliedItemBuff>().GetManagedEnumerator();

        public TooltipConstructor KeywordsTooltip {
            get {
                var tokenText = new TooltipConstructorTokenText();
                bool hasAny = false;
 
                foreach (var keyword in SkillsUtils.KeywordDescriptions(Item.Template.Description, Item.Keywords)) {
                    hasAny = true;
                    tokenText.AddToken(new TokenText(TokenType.TooltipText, keyword));
                }

                if (!hasAny) {
                    return null;
                } else {
                    return tokenText.GetTooltip(Item.Owner?.Character, Item);
                }
            }
        }

        public virtual int Price => Item.Price;
        public float Weight => Item.Weight;
        public bool IsEquipped => Item.IsEquipped;
        public bool RequirementsMet => Item.StatsRequirements?.RequirementsMet ?? true;
        public bool HasSkills => Item.ItemEffectsSkills.Any();
        public bool IsMagic => Item.IsMagic;

        public EquipmentType EquipmentType => Item.TryGetElement<ItemEquip>()?.EquipmentType;

        public Item ExistingItem => Item;
        public bool IsStolen => StolenItemElement.IsStolen(Item);
        public string StolenText => StolenItemElement.StolenText(Item);
        public ItemSeed ItemSeed => Item.TryGetElement<ItemSeed>();
        public PlantSlot PlantSlot { get; [UnityEngine.Scripting.Preserve] set; }
    }
}