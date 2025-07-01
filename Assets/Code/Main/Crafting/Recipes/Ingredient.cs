using System;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Crafting.Recipes {
    [Serializable]
    public class Ingredient {
        [TemplateType(typeof(ItemTemplate))]
        public TemplateReference templateReference;
        [SerializeField]
        int count;

        public ItemTemplate Template => templateReference.Get<ItemTemplate>();

        public int Count {
            get => Mathf.Max(1, GetModifiedCount());
            init => count = value;
        }
        public bool Match(Item item) {
            if (Template == null) {
                Log.Important?.Error($"Ingredient with null templateReference, guid: {templateReference.GUID}");
                return false;
            }
            
            if (item.Template != null) {
                if (Template.IsAbstract) {
                    return item.Template.AbstractTypes.CheckContainsAndRelease(Template);
                }

                return item.Template == Template;
            }
            return false;
        }

        public bool Match(ItemTemplate template) {
            if (Template == null) {
                Log.Important?.Error($"Ingredient with null templateReference, guid: {templateReference.GUID}");
                return false;
            }

            if (template != null) {
                if (Template.IsAbstract) {
                    return template.AbstractTypes.CheckContainsAndRelease(Template);
                }

                return template == Template;
            }

            return false;
        }
        
        int GetModifiedCount() {
            float modifiedCount = this.count;
            modifiedCount -= Hero.Current.HeroStats.CraftingRequirementModifier.ModifiedInt;
            if (Hero.Current.Development.ConsumeLessAlcoholInAlchemy && Template.IsAlcohol) {
                modifiedCount *= GameConstants.Get.consumeLessAlcoholInAlchemyMultiplier;
            }
            return (int) math.ceil(modifiedCount);
        }

    }
}