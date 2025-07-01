using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Stories.Quests {
    public class AchievementTrackingService : IDomainBoundService {
        const int UnleashTheLegendKillsRequired = 5;
        
        AchievementsReferences _achievements;
        int _wyrdSkillKillsCounter;
        IEventListener _onKillListener;
        GameplayMemory _gameplayMemory;
        List<IEventListener> _listeners = new();
        
        public Domain Domain => Domain.Gameplay;
        
        public bool RemoveOnDomainChange() {
            if (_listeners.Count > 0) {
                var listeners = _listeners.ToArray();
                for (int i = listeners.Length - 1; i >= 0; i--) {
                    World.EventSystem.DisposeListener(ref listeners[i]);
                }
            }
            
            _listeners.Clear();
            return true;
        }
            
        public void Init() {
            _gameplayMemory = World.Services.Get<GameplayMemory>();
            _achievements = CommonReferences.Get.achievementsReferences;
            
            ListenForAchievement(Hero.Events.Died, _achievements.waaaoooaagghhAchievement, TryCompleteWaaaoooaagghh);
            ListenForAchievement(Hero.Events.WyrdskillToggled, _achievements.unleashTheLegend, TryCompleteUnleashTheLegend);
            ListenForAchievement(BuildupStatus.Events.BuildupCompleted, _achievements.ironic, TryCompleteIronic);
            ListenForAchievement(PlantSlot.Events.FullyGrownPlantHarvested, _achievements.itsAlive);
            ListenForAchievement(Hero.Events.HouseBought, _achievements.homeSweetHome);
            ListenForAchievement(RestPopupUI.Events.RestingInterrupted, _achievements.preyOfTheWyrd);
            ListenForAchievement(HeroPetSharg.Events.PetShargEnded, _achievements.theCostOfCuriosityAchievement);
            ListenForAchievement(StrawDadCombat.Events.BurningEnded, _achievements.shouldntHaveDoneThat);
        }

        void TryCompleteIronic(BuildupStatus s) {
            bool isRumplot = s.Character is NpcElement npc && npc.Template.InheritsFrom(_achievements.rumpolt.Get<NpcTemplate>());
            bool isCheeseStatus = s.Template.InheritsFrom(_achievements.cheeseStatus.Get<StatusTemplate>());
            if (isRumplot && isCheeseStatus) {
                GetAchievement(_achievements.ironic);
            }
        }
        
        void ListenForAchievement<TSource, TPayload>(IEvent<TSource, TPayload> iEvent, TemplateReference achievement, Action<TPayload> action = null) {
            if (AchievementCompleted(achievement)) {
                return;
            }

            action ??= _ => GetAchievement(achievement);
            _listeners.Add(World.EventSystem.ListenTo(EventSelector.AnySource, iEvent, this, action));
        }

        bool AchievementCompleted(TemplateReference achievement) {
            return QuestUtils.StateOfQuestWithId(_gameplayMemory, achievement) is QuestState.Completed;
        }
        
        void TryCompleteWaaaoooaagghh(DamageOutcome damageOutcome) {
            var heroHasIcarusRingEquipped = Hero.Current.Inventory.Items.Any(item => item.Template == CommonReferences.Get.IcarusRingTemplate && item.IsEquipped);

            if (damageOutcome.Damage.Type is DamageType.Fall && heroHasIcarusRingEquipped) {
                GetAchievement(_achievements.waaaoooaagghhAchievement);
            }
        }

        void TryCompleteUnleashTheLegend(bool toggled) {
            if (toggled) {
                _onKillListener = Hero.Current.ListenTo(HealthElement.Events.OnKill, OnKill, Hero.Current);
            } else {
                World.EventSystem.DisposeListener(ref _onKillListener);
                _wyrdSkillKillsCounter = 0;
            }
        }

        void OnKill(DamageOutcome damageOutcome) {
            if (damageOutcome.Attacker is not Hero || damageOutcome.Target is not NpcElement) {
                return;
            }
            
            _wyrdSkillKillsCounter++;

            if (_wyrdSkillKillsCounter >= UnleashTheLegendKillsRequired) {
                GetAchievement(_achievements.unleashTheLegend);
            }
        }

        void GetAchievement(TemplateReference achievement) {
            QuestState currentState = QuestUtils.StateOfQuestWithId(World.Services.Get<GameplayMemory>(), achievement);

            if (currentState == QuestState.NotTaken) {
                // Auto start quests before completing them
                Quest quest = new(achievement.Get<QuestTemplateBase>());
                World.Add(quest);
                currentState = quest.State;
            }

            if (currentState == QuestState.Active) {
                QuestUtils.Complete(achievement, true);
            }
        }
    }
}