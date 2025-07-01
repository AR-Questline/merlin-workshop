using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC.Utils;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Block {
    public partial class BlockPommel : BlockStateBase {
        public const byte PommelStaminaDamage = 5;
        
        bool _triggered;
        Vector3 _pommelDirection = Vector3.zero;
        WeakModelRef<Item> _itemRef;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Block;
        public override HeroStateType Type => HeroStateType.BlockPommel;
        public override HeroStateType StateToEnter => UseBlockWithoutShield ? HeroStateType.BlockPommelWithoutShield : HeroStateType.BlockPommel;
        public override bool CanPerformNewAction => false;
        public override float EntryTransitionDuration => 0.0f;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _triggered = false;
            ParentModel.ResetAttackProlong();
            PreventStaminaRegen();
            Hero.RemoveElementsOfType<HeroBlock>();
            _itemRef = Hero.BlockRelatedStats.ParentModel;
        }

        protected override void OnUpdate(float deltaTime) {
            if (!_triggered && TimeElapsedNormalized > 0.2f) {
                _pommelDirection = Hero.VHeroController.Pommel(HeroBlock.GetStatsItem(Hero.Current));
                HeroBlock.GetBlockingWeapon(Hero)?.PlayAudioClip(ItemAudioType.PommelSwing);
                _triggered = true;
            }
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(ParentModel.BlockHeld ? HeroStateType.BlockLoop : HeroStateType.Idle);
            }
        }

        protected override void OnExit(bool restarted) {
            DisableStaminaRegenPrevent();
            if (_itemRef.TryGet(out Item item)) {
                var attackParameters = new AttackParameters(Hero, item, AttackType.Pommel, _pommelDirection);
                Hero.TryGetElement<IHandOwner<Hero>>()?.OnAttackEnded(attackParameters);
            }
        }
    }
}