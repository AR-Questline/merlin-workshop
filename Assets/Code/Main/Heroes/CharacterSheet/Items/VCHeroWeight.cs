using System.Globalization;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items {
    public class VCHeroWeight : ViewComponent {
        [SerializeField] TextMeshProUGUI text;

        static Hero Hero => Hero.Current;
        HeroItems _heroItems;

        float CurrentWeight => _heroItems.CurrentWeight;
        float MaxWeight => _heroItems.ParentModel.HeroStats.EncumbranceLimit.ModifiedInt;
        
        protected override void OnAttach() {
            _heroItems = Hero.HeroItems;
            Hero.ListenTo(IItemOwner.Relations.Owns.Events.Changed, Refresh, this);
            Hero.ListenTo(Stat.Events.StatChanged(HeroStatType.EncumbranceLimit), Refresh, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, Item.Events.QuantityChanged, this, Refresh);
            World.EventSystem.ListenTo(EventSelector.AnySource, HeroEncumbered.Events.EncumberedChanged, this, Refresh);
            
            Refresh();
        }

        void Refresh() {
            string currentWeight = Mathf.Ceil(CurrentWeight).ToString(CultureInfo.InvariantCulture).ColoredTextIf(Hero.IsEncumbered, ARColor.MainAccent);
            text.text = $"{currentWeight}/{MaxWeight}";
        }
    }
}