using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Combat {
    public class FinisherType : RichEnum {
        readonly Action<Hero, Item> _spawnFinisher;
        
        [UnityEngine.Scripting.Preserve]
        public static readonly FinisherType
            None = new(nameof(None), null),
            DamageWave = new(nameof(DamageWave), (e, i) => e.AddElement(new FinisherDamageWave(i)));
        
        protected FinisherType(string enumName, Action<Hero, Item> spawnFinisher, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            _spawnFinisher = spawnFinisher;
        }

        public void SpawnFinisher(Hero hero, Item item) {
            hero.RemoveElementsOfType<IFinisher>();
            _spawnFinisher?.Invoke(hero, item);
        }
    }
}
