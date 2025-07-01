using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Locations.Actions.Customs;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class GameplayActionTracker : BaseSimpleTracker<GameplayActionTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.GameplayActionTracker;

        Action _trackedAction;
        AchievementsReferences _achievements;

        public override void InitFromAttachment(GameplayActionTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _trackedAction = spec.TrackedAction;
        }

        protected override void OnInitialize() {
            ListenToTrackedAction();
            _achievements = CommonReferences.Get.achievementsReferences;
        }

        IEventListener ListenToTrackedAction() {
            return _trackedAction switch {
                Action.Crafting or Action.Brewing or Action.Cooking =>
                    Hero.Current.ListenTo(Crafting.Crafting.Events.Created, OnItemCrafted, this),
                Action.Lockpicking =>
                    World.EventSystem.ListenTo(EventSelector.AnySource,
                        LockpickingInteraction.Events.Unlocked, this, CompleteTrackedAction),
                Action.JournalUnlocking =>
                    World.EventSystem.ListenTo(EventSelector.AnySource,
                        PlayerJournal.Events.EntryUnlocked, this, CompleteTrackedAction),
                Action.UniqueFishCatching =>
                    Hero.Current.ListenTo(HeroCaughtFish.Events.CaughtNew, CompleteTrackedAction, this),
                Action.WeakspotHit =>
                    World.EventSystem.ListenTo(EventSelector.AnySource,
                        HealthElement.Events.OnWeakspotHit, this, CompleteTrackedAction),
                Action.SummonedWolfKilledWolf =>
                    World.EventSystem.ListenTo(EventSelector.AnySource,
                        HealthElement.Events.OnHeroSummonKill, this, OnHeroSummonKill),
                Action.StagfatherTrialCompleted =>
                    World.EventSystem.ListenTo(EventSelector.AnySource,
                        ITrialElement.Events.TrialEnded, this, OnTrialEnded),
                _ => null
            };
        }

        void OnHeroSummonKill(DamageOutcome damageOutcome) {
            bool wasWolfKilled = false;
            bool wasHeroWolfSummonAttacker = false;

            if (damageOutcome.Target is NpcElement target) {
                foreach (var wolf in _achievements.wolves) {
                    if (target.Template.InheritsFrom(wolf.Get<NpcTemplate>())) {
                        wasWolfKilled = true;
                        break;
                    }
                }
            }
            
            if (damageOutcome.Attacker is NpcElement attacker) {
                foreach (var summonWolf in _achievements.summonWolves) {
                    if (attacker.Template.InheritsFrom(summonWolf.Get<NpcTemplate>())) {
                        wasHeroWolfSummonAttacker = true;
                        break;
                    }
                }
            }
            
            if (wasWolfKilled && wasHeroWolfSummonAttacker) {
                CompleteTrackedAction();
            }
        }

        void OnItemCrafted(CreatedEvent createdEvent) {
            bool requirement = _trackedAction switch {
                Action.Brewing => createdEvent.Item is { IsPotion: true },
                Action.Cooking => createdEvent.Item is { IsDish: true } or { IsPlainFood: true },
                Action.Crafting => createdEvent.Item is { IsDish: false, IsPlainFood: false, IsPotion: false },
                _ => false
            };

            if (requirement) {
                CompleteTrackedAction();
            }
        }

        void OnTrialEnded(bool successfully) {
            if (successfully) {
                CompleteTrackedAction();
            }
        }

        void CompleteTrackedAction() {
            ChangeBy(1f);
        }

        public enum Action : byte {
            Crafting,
            Cooking,
            Brewing,
            Lockpicking,
            JournalUnlocking,
            UniqueFishCatching,
            StagfatherTrialCompleted,
            WeakspotHit,
            SummonedWolfKilledWolf
        }
    }
}