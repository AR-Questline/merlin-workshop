using System;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class TimeOfDayDependentSkills : Element<Location>, IRefreshedByAttachment<TimeOfDayDependentSkillsAttachment> {
        public override ushort TypeForSerialization => SavedModels.TimeOfDayDependentSkills;

        // === Fields & Properties
        SkillsActivityData[] _skillsActivityData;
        TimedEvent _nextActivityChangeEvent;

        public void InitFromAttachment(TimeOfDayDependentSkillsAttachment spec, bool isRestored) {
            _skillsActivityData = ArrayUtils.Select(spec.TimeBasedSkills, tbs => new SkillsActivityData {
                timeOfDayDependentSkill = tbs,
                isActive = false,
                createdSkills = null
            });
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(UpdateActivity);
        }

        void UpdateActivity() {
            var characterSkills = GetCharacterSkills();
            if (characterSkills == null) {
                return;
            }
            
            var currentTime = World.Any<GameRealTime>().WeatherTime;
            
            for (int i = 0; i < _skillsActivityData.Length; i++) {
                bool shouldBeActive = _skillsActivityData[i].timeOfDayDependentSkill.IsInTimeCycle(currentTime);
                if (shouldBeActive == _skillsActivityData[i].isActive) {
                    continue;
                }
                _skillsActivityData[i].isActive = shouldBeActive;
                if (shouldBeActive) { 
                    _skillsActivityData[i].createdSkills = InitSkills(characterSkills, _skillsActivityData[i].timeOfDayDependentSkill);
                } else {
                    if (_skillsActivityData[i].createdSkills == null) {
                        continue;
                    }
                    foreach (var createdSkill in _skillsActivityData[i].createdSkills) {
                        if (createdSkill is { HasBeenDiscarded: false }) {
                            createdSkill.Discard();
                        }
                    }
                    _skillsActivityData[i].createdSkills = null;
                }
            }

            UpdateActivityEvent();
        }

        void UpdateActivityEvent() {
            var gameTimeEvents = World.Any<GameTimeEvents>();
            if (gameTimeEvents == null) {
                return;
            }
            
            if (_nextActivityChangeEvent != null) {
                gameTimeEvents.RemoveEvent(_nextActivityChangeEvent);
            }

            var nextTime = GetClosestTime(_skillsActivityData, World.Any<GameRealTime>().WeatherTime);
            _nextActivityChangeEvent = new TimedEvent(nextTime, UpdateActivity);
            gameTimeEvents.AddEvent(_nextActivityChangeEvent);
        }

        static Skill[] InitSkills(ICharacterSkills characterSkills, TimeOfDayDependentSkillsAttachment.TimeOfDayDependentSkill timeOfDayDependentSkill) {
            return ArrayUtils.Select(timeOfDayDependentSkill.Skills, skill => {
                var skillElement = characterSkills.LearnSkill(skill.CreateSkill());
                skillElement.MarkedNotSaved = true;
                return skillElement;
            });
        }

        ICharacterSkills GetCharacterSkills() {
            if (ParentModel is null or { HasBeenDiscarded: true }) {
                return null;
            } else if (ParentModel.TryGetElement<NpcElement>(out var npc)) {
                return npc.Skills;
            } else if (ParentModel.TryGetElement<Hero>(out var hero)) {
                return hero.Skills;
            } else {
                return null;
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            var gameTimeEvents = World.Any<GameTimeEvents>();
            if (gameTimeEvents != null) {
                gameTimeEvents.RemoveEvent(_nextActivityChangeEvent);
            } 
            _nextActivityChangeEvent = null;
        }

        static DateTime GetClosestTime(IEnumerable<SkillsActivityData> skillsActivityData, ARDateTime currentTime) {
            var currentDay = new ARDateTime(currentTime.Date.Date);
            var closestTime = currentTime.Date + TimeSpan.FromDays(1);
            foreach (var activityData in skillsActivityData) {
                var fromTime = currentDay + activityData.timeOfDayDependentSkill.FromTime();
                var toTime = currentDay + activityData.timeOfDayDependentSkill.ToTime();
                if (toTime < fromTime) {
                    toTime += TimeSpan.FromDays(1);
                }
                
                if (fromTime <= currentTime) {
                    fromTime += TimeSpan.FromDays(1);
                }
                if (toTime <= currentTime) {
                    fromTime += TimeSpan.FromDays(1);
                }
                
                if (fromTime < closestTime) {
                    closestTime = fromTime.Date;
                }
                if (toTime < closestTime) {
                    closestTime = toTime.Date;
                }
            }
            return closestTime;
        }

        class SkillsActivityData {
            public TimeOfDayDependentSkillsAttachment.TimeOfDayDependentSkill timeOfDayDependentSkill;
            public bool isActive;
            public Skill[] createdSkills;
        }
    }
}
