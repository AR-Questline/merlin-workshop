using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class FinisherDamageWave : Element<Hero>, IFinisher {
        public sealed override bool IsNotSaved => true;

        public float Damage => Item.ItemStats.DamageValue;
        public float ForceDamage => Item.ItemStats.ForceDamage;
        public float RagdollForce => Item.ItemStats.RagdollForce;
        public RuntimeDamageTypeData DamageTypeData => Item.ItemStats.RuntimeDamageTypeData;
        public Item Item { get; }
        
        public FinisherDamageWave(Item item) {
            Item = item;
        }

        protected override void OnInitialize() {
            Item.ListenTo(Events.AfterDiscarded, Discard, this);
        }

        public void Release(Vector3 position) {
            VFinisherDamageWave view = World.SpawnView<VFinisherDamageWave>(this);
            view.Init(position).Forget();
        }
    }
}