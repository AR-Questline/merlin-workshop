using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that should be converted to currency.")]
    public class ItemToCurrencySpec : MonoBehaviour, IAttachmentSpec {
        [SerializeField, RichEnumExtends(typeof(CurrencyStatType))] 
        RichEnumReference currency;

        public CurrencyStatType Currency => currency.EnumAs<CurrencyStatType>();

        public float multiplier = 1;

        public Element SpawnElement() {
            return new ItemToCurrency();
        }

        public bool IsMine(Element element) {
            if (element is ItemToCurrency { stat: { } sRef}) {
                return sRef == Currency;
            }
            return false;
        }
    }
}