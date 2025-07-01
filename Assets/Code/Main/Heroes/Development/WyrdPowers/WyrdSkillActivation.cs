using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.Development.WyrdPowers {
    public partial class WyrdSkillActivation : Element<HeroDevelopment>, IUIPlayerInput {
        public override ushort TypeForSerialization => SavedModels.WyrdSkillActivation;

        const float EpsilonThreshold = 0.001f;

        LimitedStat _wyrdSkillDuration;

        public bool IsDepleted => _wyrdSkillDuration <= 0;
        public bool CanBeActivated => HasAnyWyrdSkill && !IsDepleted && !IsActive;
        public bool IsActive => WyrdSoulFragments.IsActive;
        
        Hero Hero => ParentModel.ParentModel;
        WyrdSoulFragments WyrdSoulFragments => ParentModel.WyrdSoulFragments;
        bool HasAnyWyrdSkill => WyrdSoulFragments.HasAnySkill;
        
        public IEnumerable<KeyBindings> PlayerKeyBindings {
            get {
                yield return KeyBindings.Gameplay.UseWyrdSkillsSlot;
                yield return KeyBindings.Gameplay.AlternativeUseWyrdSkillsSlot;
            }
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(Init, this);
        }

        void Init() {
            _wyrdSkillDuration = Hero.WyrdSkillDuration;
            World.Only<PlayerInput>().RegisterPlayerInput(this, this);
            Hero.GetOrCreateTimeDependent().WithUpdate(Update);
            Hero.ListenTo(Stat.Events.ChangingStat(HeroStatType.WyrdSkillDuration), OnWyrdSkillDurationChanged, this);
        }

        public void RestoreSkillDuration() {
            _wyrdSkillDuration.SetToFull();
            FancyPanelType.Custom.Spawn(this, LocTerms.WyrdSkillRestoreInfo.Translate());
        }

        void Update(float deltaTime) {
            if (IsActive) {
                _wyrdSkillDuration.DecreaseBy(deltaTime);
                if (_wyrdSkillDuration <= EpsilonThreshold) {
                    Deactivate();
                }
            }
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction && HasAnyWyrdSkill) {
                if (IsActive) {
                    Deactivate();
                    return UIResult.Accept;
                } else if (!IsDepleted) {
                    Activate();
                    return UIResult.Accept;
                } else {
                    Hero.Trigger(Hero.Events.StatUseFail, HeroStatType.WyrdSkillDuration);
                    return UIResult.Ignore;
                }
            }

            return UIResult.Ignore;
        }

        public bool TryActivate() {
            if (CanBeActivated) {
                Activate();
                return true;
            }
            return false;
        }

        void Activate() {
            WyrdSoulFragments.ActivatePowers();
            Hero.Trigger(Hero.Events.WyrdskillToggled, true);
        }

        void Deactivate() {
            _wyrdSkillDuration.SetTo(0);
            WyrdSoulFragments.DeactivatePowers();
            Hero.Trigger(Hero.Events.WyrdskillToggled, false);
        }

        void OnWyrdSkillDurationChanged(HookResult<IWithStats, Stat.StatChange> statChangeHook) {
            var statChange = statChangeHook.Value;
            // Prevent increasing the duration if the skill is active
            if (statChange.value > statChange.originalValue && IsActive && statChange.context.reason != ChangeReason.Forceful) {
                statChangeHook.Prevent();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Hero.GetTimeDependent()?.WithoutUpdate(Update);
        }
    }
}