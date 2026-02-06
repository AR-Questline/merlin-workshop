using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class AliveLocationDeathReward : Element<Location>, IRefreshedByAttachment<AliveLocationDeathRewardAttachment> {
        public override ushort TypeForSerialization => SavedModels.AliveLocationDeathReward;

        AliveLocationDeathRewardAttachment _spec;
        
        public void InitFromAttachment(AliveLocationDeathRewardAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            ParentModel.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.BeforeDeath, OnBeforeDeath, this);
        }
        
        void OnBeforeDeath(DamageOutcome outcome) {
            if (_spec.hasToBeKilledByHero && outcome.Attacker is not Hero and not NpcElement { IsHeroSummon: true }) {
                return;
            }

            var heroItems = Hero.Current.HeroItems;
            foreach (var itemSpawningDataRuntime in _spec.reward.LootTable(Hero.Current).PopLoot(Hero.Current).items) {
                heroItems.Add(new Item(itemSpawningDataRuntime));
            }
        }
    }
}