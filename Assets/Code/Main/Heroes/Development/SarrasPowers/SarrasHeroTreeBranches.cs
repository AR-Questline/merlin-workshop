using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Heroes.Development.SarrasPowers {
    public partial class SarrasHeroTreeBranches : Element<HeroDevelopment> {
        public override ushort TypeForSerialization => SavedModels.SarrasHeroTreeBranches;

        [Saved] public bool IsUnlocked { get; private set; }
        [Saved] public bool IsFirstCharged { get; private set; }
        [Saved] public TalentTreeBranchType CurrentlySelected { get; private set; }
        
        public new static class Events {
            public static readonly Event<SarrasHeroTreeBranches, TalentTreeBranchType> TalentTreeBranchChanged = new(nameof(TalentTreeBranchChanged));
            public static readonly Event<SarrasHeroTreeBranches, bool> FirstChargeCommitted = new(nameof(FirstChargeCommitted));
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<SarrasTalentOverviewUI>(), this, SarrasTreeOverviewAdded);
            if (!IsUnlocked) {
                Hero.Current.ListenToLimited(Stat.Events.StatChanged(CharacterStatType.CatalystTalentPoints), () => {
                    IsUnlocked = true;
                }, this);
            }
        }

        protected override void OnRestore() {
            base.OnRestore();
            LoadSave.Get.LoadSystem.AfterGameRestored(() => {
                SetupTalents(Hero.Current.Talents.TableOf(CommonReferences.Get.SarrasTalentTableTemplate));
            });
        }

        public void CommitFirstCharge() {
            if (IsFirstCharged) {
                return;
            }
            
            IsFirstCharged = true;
            this.Trigger(Events.FirstChargeCommitted, true);
        }

        public void SelectTalentTreeBranch(TalentTreeBranchType branchType) {
            CurrentlySelected = branchType;
            this.Trigger(Events.TalentTreeBranchChanged, branchType);
        }
        
        public void NextBranch() {
            if (CurrentlySelected == TalentTreeBranchType.None || !UIStateStack.Instance.State.IsMapInteractive) {
                return;
            }
            
            var next = CurrentlySelected switch {
                TalentTreeBranchType.SarrasMage => TalentTreeBranchType.SarrasRogue,
                TalentTreeBranchType.SarrasRogue => TalentTreeBranchType.SarrasWarrior,
                TalentTreeBranchType.SarrasWarrior => TalentTreeBranchType.SarrasMage,
                _ => CurrentlySelected
            };
            SelectTalentTreeBranch(next);
            SetupTalents(Hero.Current.Talents.TableOf(CommonReferences.Get.SarrasTalentTableTemplate));
            HandleNotification();
        }

        void HandleNotification() {
            var notificationText = LocTerms.UISarrasTreeChanged.Translate(GetCurrentBranchName());
            var notification = World.Any<LowerInfoNotification>();
            
            if (notification) {
                notification.OverrideText(notificationText);
            } else {
                notification = new LowerInfoNotification(notificationText, typeof(VLowerInfoNotification));
                NotificationUtils.PushExplicitly<LowerMiddleScreenNotificationBuffer, LowerInfoNotification>(notification);
            }
        }
        
        string GetCurrentBranchName() {
            return CurrentlySelected switch {
                TalentTreeBranchType.SarrasMage => LocTerms.SarrasMageTalentPoints.Translate(),
                TalentTreeBranchType.SarrasRogue => LocTerms.SarrasRogueTalentPoints.Translate(),
                TalentTreeBranchType.SarrasWarrior => LocTerms.SarrasWarriorTalentPoints.Translate(),
                _ => string.Empty
            };
        }

        void SarrasTreeOverviewAdded(Model model) {
            if (model is SarrasTalentOverviewUI overviewUI) {
                overviewUI.ListenToLimited(Model.Events.BeforeDiscarded, SarrasTreeOverviewDiscarded, this);
            }
        }

        void SarrasTreeOverviewDiscarded(Model model) {
            if (model is not SarrasTalentOverviewUI overviewUI) {
                return;
            }
            
            SetupTalents(overviewUI.TalentTreeUI.CurrentTable);
        }

        void SetupTalents(TalentTable sarrasTalentTable) {
            foreach (Talent talent in sarrasTalentTable.talents) {
                var branchType = talent.TalentTreeBranchType.ToSarrasTreeBranchType();
                if (branchType == TalentTreeBranchType.None) {
                    Log.Critical?.Error("A talent in Sarras Talent Tree has no branch type assigned. Please check the template.");
                    talent.Reset();
                    continue;
                }
                
                bool isUpgraded = talent.IsUpgraded;
                if (isUpgraded && branchType != CurrentlySelected) {
                    talent.RemoveCurrentSkills();
                }

                if (isUpgraded && branchType == CurrentlySelected) {
                    talent.RefreshSkills();
                }
            }
        }
    }
}