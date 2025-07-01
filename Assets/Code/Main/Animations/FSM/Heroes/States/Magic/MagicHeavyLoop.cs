using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicHeavyLoop : HeroAnimatorState<MagicFSM> {
        IEventListener _eventListener;

        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastHeavy;
        public override HeroStateType Type => HeroStateType.MagicHeavyLoop;
        public override bool UsesActiveLayerMask => true;

        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.SynchronizedStateOffsetNormalizedTime();
        
        public new class Events {
            public static readonly Event<Hero, Item> BeforeSpawnedNewItemInHand = new(nameof(BeforeSpawnedNewItemInHand));
        }
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.ResetProlong();
            ParentModel.PlayAudioClip(ItemAudioType.CastFullyCharged.RetrieveFrom(ParentModel.Item));
            _eventListener = Hero.ListenTo(Events.BeforeSpawnedNewItemInHand, OnBeforeSpawnedNewItemInHand, this);
        }

        protected override void OnUpdate(float deltaTime) {
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {effects = GameConstants.Get.magicHeavyLoopXboxVibrations, handsAffected = ParentModel.CastingHand});
        }

        protected override void OnExit(bool restarted) {
            World.EventSystem.DisposeListener(ref _eventListener);
            base.OnExit(restarted);
        }

        void OnBeforeSpawnedNewItemInHand(Item item) {
            ParentModel.Item.ActiveSkills.ForEach(skill => skill.Refund());
            ParentModel.EndSlowModifier();
        }
    }
}