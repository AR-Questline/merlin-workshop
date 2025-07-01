#if !UNITY_GAMECORE && !UNITY_PS5
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Analytics {
    public partial class HeroAnalytics : Element<GameAnalyticsController> {
        public sealed override bool IsNotSaved => true;

        string NiceName(string id, int limit = 32) => AnalyticsUtils.EventName(id, limit);
        int HeroLevel => AnalyticsUtils.HeroLevel;
        float PlayTime => AnalyticsUtils.PlayTime;
        
        // === Initialization
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, CharacterCreator.Events.CharacterCreated, this, OnHeroCreated);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<Hero>(), this, OnHeroInit);
            World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentConfirmed, this, OnTalentAcquired);
            World.EventSystem.ListenTo(EventSelector.AnySource, Location.Events.Interacted, this, OnHeroInteracted);
            World.EventSystem.ListenTo(EventSelector.AnySource, UnconsciousElement.Events.LoseConscious, this, OnEnemyLoseConscious);
            World.EventSystem.ListenTo(EventSelector.AnySource, UnconsciousElement.Events.UnconsciousKilled, this, OnUnconsciousEnemyKilled);
        }

        // === Callbacks
        void OnHeroInit(Model model) {
            Hero hero = (Hero) model;
            
            //Progression
            hero.ListenTo(Hero.Events.LevelUp, OnLevelUp);

            foreach (ProfStatType profStat in ProfStatType.HeroProficiencies) {
                hero.ListenTo(Stat.Events.StatChangedBy(profStat), OnProficiencyAcquired, this);
            }

            hero.ListenTo(Stat.Events.StatChangedBy(HeroRPGStatType.Strength), OnRPGStatAcquired, this);
            hero.ListenTo(Stat.Events.StatChangedBy(HeroRPGStatType.Dexterity), OnRPGStatAcquired, this);
            hero.ListenTo(Stat.Events.StatChangedBy(HeroRPGStatType.Spirituality), OnRPGStatAcquired, this);
            hero.ListenTo(Stat.Events.StatChangedBy(HeroRPGStatType.Endurance), OnRPGStatAcquired, this);
            hero.ListenTo(Stat.Events.StatChangedBy(HeroRPGStatType.Perception), OnRPGStatAcquired, this);
            hero.ListenTo(Stat.Events.StatChangedBy(HeroRPGStatType.Practicality), OnRPGStatAcquired, this);
            
            //Death
            hero.ListenTo(Hero.Events.Died, OnHeroDeath, this);
            
            //Kill
            hero.ListenTo(HealthElement.Events.OnKill, OnHeroKilledSomething, this);
            
            //Wyrdskill
            hero.ListenTo(Hero.Events.WyrdSoulFragmentCollected, OnWyrdSoulFragmentCollected, this);
            hero.ListenTo(Hero.Events.WyrdskillToggled, OnWyrdskillToggled, this);
            
            //Crimes
            hero.ListenTo(CrimePenalties.Events.CrimePenaltyGuardCaught, OnCrimeGuardCaught, this);
            hero.ListenTo(CrimeUtils.Events.CrimeCommitted, OnCrimeCommitted, this);
            hero.ListenTo(CrimePenalties.Events.CrimePenaltyPayedBounty, OnCrimePenaltyPayedBounty, this);
            hero.ListenTo(CrimePenalties.Events.CrimePenaltyWentToJailFromCombat, OnCrimePenaltyWentToJailFromCombat, this);
            hero.ListenTo(CrimePenalties.Events.CrimePenaltyWentToJailPeacefully, OnCrimePenaltyWentToJailPeacefully, this);
        }

        void OnHeroCreated(CharacterCreator creator) {
            string difficulty = World.Only<DifficultySetting>().Difficulty.EnumName;
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:Difficulty:{difficulty}");
            
            string template = "None";
            if (creator.BuildPreset.name is { } name) {
                template = name.IdOverride;
                if (template.IsNullOrWhitespace()) {
                    template = name.ID;
                }
                template = NiceName(template);
            }
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:StartingTemplate:{template}");
            
            string gender = creator.GetGender() == Gender.Male ? "Male" : "Female";
            int preset = creator.GetPresetIndex();
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:StartingPreset:{(preset == -1 ? "Custom" : preset)}");
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:HeadShape:{creator.GetHeadShapeIndex()}");
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:SkinColour:{creator.GetSkinColorIndex()}");
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:Hair:{creator.GetHairIndex()}:{creator.GetHairColorIndex()}");
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:Beard:{creator.GetBeardIndex()}:{creator.GetBeardColorIndex()}");
            AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:BodyNormals:{creator.GetBodyNormalsIndex()}");
            
            // Not used right now
            // AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:BodyMuscular:{creator.GetMuscularIndex()}");
            // AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:BodyFat:{creator.GetFatIndex()}");
            // AnalyticsUtils.TrySendDesignEvent($"HeroCreated:{gender}:BodyOld:{creator.GetOldIndex()}");
        }

        void OnLevelUp(int level) {
            if (CharacterCreator.applyingBuildPreset) {
                return;
            }
            
            string newLevelString = level.ToString();
            if (level > 100) {
                newLevelString = "100+";
            }
            string evt = $"LevelUp:{newLevelString}";
            Hero hero = Hero.Current;
            
            // PlayTime is currently broken because of presents and Wealth is not important.
            // Disabled because we need to limit the amount of events sent.
            // AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:PlayTime", PlayTime);
            // AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:Wealth", hero.Wealth.ModifiedInt);
            
            int amountOfProficiencies = 0;
            int amountOfIncreasedProficiencies = 0;
            float avgProficiency = 0;
            float avgIncreasedProficiency = 0;
            foreach (var profStatType in ProfStatType.HeroProficiencies) {
                Stat stat = hero.Stat(profStatType);
                if (stat != null) {
                    amountOfProficiencies++;
                    float statValue = stat.ModifiedInt;
                    avgProficiency += statValue;
                    if (statValue > ProficiencyStats.ProficiencyBaseValue) {
                        amountOfIncreasedProficiencies++;
                        avgIncreasedProficiency += statValue;
                    }
                }
            }
            
            avgProficiency /= amountOfProficiencies;
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:AvgProficiency", avgProficiency);

            if (amountOfIncreasedProficiencies > 0) {
                avgIncreasedProficiency /= amountOfIncreasedProficiencies;
                AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:AvgIncreasedProficiency", avgIncreasedProficiency);
            }
        }

        void OnTalentAcquired(Talent.ChangeData data) {
            if (CharacterCreator.applyingBuildPreset) {
                return;
            }
            
            var talent = data.talent;
            if (talent.Level == 0) {
                return;
            }
            
            TalentTable table = talent.ParentModel;
            string talentTableName = NiceName(table.TreeTemplate.name);
            
            int levelBefore = talent.Level - data.levelGain;
            int levelAfter = talent.Level;
            for (int i = levelBefore + 1; i <= levelAfter; i++) {
                string talentName = $"{NiceName(talent.Template.name, 24)}_{i}";
                string evt = $"Talents:{talentTableName}:{talentName}";
                AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:HeroLevel", HeroLevel);
                AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:PointsSpent", table.PointsSpent);
            }
            // RpgStatValue is not used in talents anymore and we currently need to send less events.
            // int rpgStatValue = Hero.Current.HeroRPGStats.GetHeroRPGStats().First(stat => stat.Type == talent.ParentModel.HeroStatType).ModifiedInt;
            // if (rpgStatValue > 100) {
            //    rpgStatValue = 100;
            // }
            // AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:RPGStat", rpgStatValue);
        }

        void OnRPGStatAcquired(Stat.StatChange statChange) {
            if (CharacterCreator.applyingBuildPreset) {
                return;
            }
            
            if (statChange.value <= 0) {
                return;
            }
            Stat stat = statChange.stat;
            string statName = stat.Type.EnumName;
            string evt = $"Statistics:{statName}";
            int baseValue = (int) stat.BaseValue;
            for (int i = (int) -statChange.value + 1; i <= 0; i++) { //repeat if stat was increased many times. BaseValue and ModifiedValue is already increased by value;
                int newValue = baseValue + i;
                string newValueString = newValue.ToString();
                if (newValue > 100) {
                    newValueString = "100+";
                }
                AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:{newValueString}:HeroLevel", HeroLevel);
            }
        }
        
        void OnProficiencyAcquired(Stat.StatChange statChange) {
            if (CharacterCreator.applyingBuildPreset) {
                return;
            }
            
            string statName = statChange.stat.Type.EnumName;
            int statRangeMin = (int) (Math.Floor(statChange.stat.ModifiedValue / 5f) * 5);
            string evt = $"Statistics:{statName}:{statRangeMin}-{statRangeMin+4}"; //5-9 or 10-14 etc
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:PlayTime", PlayTime);
        }

        void OnHeroDeath(DamageOutcome outcome) {
            string cause;
            string causeName;
           
            if (outcome.Damage.DamageDealer is NpcElement npc) {
                cause = "AI";
                causeName = NiceName(npc.Template.name);
            } else {
                cause = outcome.Damage.Type switch {
                    DamageType.Environment => "Environment",
                    DamageType.Fall => "Fall",
                    DamageType.Interact => "Interact",
                    DamageType.Status => "Status",
                    DamageType.Trap => "Trap",
                    _ => "Unknown"
                };
                causeName = (outcome.Damage.DamageDealer is Hero) ? "Hero" : "Unknown";
            }
            string evt = $"DeathFrom:{cause}:{causeName}";
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:HeroLevel", HeroLevel);
        }
        
        void OnHeroKilledSomething(DamageOutcome outcome) {
            if (outcome.Target is not NpcElement npc) {
                return;
            }
            string npcName = NiceName(npc.Template.name);
            string itemType = "none";
            string itemName = "none";
            if (outcome.Damage.Item is {} item) {
                itemType = ItemsAnalytics.ItemType(item);
                itemName = ItemsAnalytics.ItemName(item.Template);
            }
            string evt = $"Killed:{npcName}:{itemType}:{itemName}";
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}", HeroLevel);
        }

        void OnHeroInteracted(LocationInteractionData interactionData) {
            if (interactionData.location == null || interactionData.character is not Hero) {
                return;
            }
            
            //pick item action is already sent in items events, search action interaction is send every frame.
            if (interactionData.location.TryGetElement<PickItemAction>()) {
                return;
            }
            if (interactionData.location.TryGetElement(out SearchAction search) && search.SearchAvailable) {
                return;
            }
            if (interactionData.location.TryGetElement(out PickpocketAction pickpocket) && pickpocket.GetAvailability(Hero.Current, interactionData.location) == ActionAvailability.Available) {
                return;
            }

            string type;
            string locationName;

            if (interactionData.location.TryGetElement(out NpcElement npcElement)) {
                type = "AI";
                locationName = NiceName(npcElement.Template.name);
            } else {
                return;
            }
            // Other types of interactions are too common to be tracked
            // } else if (interactionData.location.Template != null) {
            //     type = "Location";
            //     locationName = NiceName(interactionData.location.Template.name);    
            // } else {
            //     type = "Other";
            //     locationName = NiceName(interactionData.location.Spec.name);
            // }
            string evt = $"Interacted:{type}:{locationName}";
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:PlayTime", PlayTime);
        }

        void OnWyrdSoulFragmentCollected(WyrdSoulFragmentType fragmentType) {
            string fragmentName = NiceName(fragmentType.ToString());
            string evt = $"SoulFragment:{fragmentName}";
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:PlayTime", PlayTime);
        }
        
        void OnWyrdskillToggled(bool activated) {
            if (!activated) {
                return;
            }
            AnalyticsUtils.TrySendDesignEvent($"Hero:Wyrdskill:Activated:PlayTime", PlayTime);
        }
        
        void OnEnemyLoseConscious(UnconsciousElement unconsciousElement) {
            string name = NiceName(unconsciousElement.ParentModel.Template.name);
            string evt = $"MadeUnconscious:{name}";
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:HeroLevel", HeroLevel);
        }
        
        void OnUnconsciousEnemyKilled(UnconsciousElement unconsciousElement) {
            string name = NiceName(unconsciousElement.ParentModel.Template.name);
            string evt = $"ExecutedUnconscious:{name}";
            AnalyticsUtils.TrySendDesignEvent($"Hero:{evt}:HeroLevel", HeroLevel);
        }

        void OnCrimeGuardCaught(CrimeOwnerTemplate crimeOwner) {
            string factionName = NiceName(crimeOwner.name);
            int bounty = (int) CrimeUtils.Bounty(crimeOwner);
            AnalyticsUtils.TrySendDesignEvent($"Hero:Crime:{factionName}:GuardCaught:Bounty", bounty);
        }
        
        void OnCrimePenaltyWentToJailPeacefully(CrimeOwnerTemplate crimeOwner) {
            string factionName = NiceName(crimeOwner.name);
            int bounty = (int) CrimeUtils.Bounty(crimeOwner);
            AnalyticsUtils.TrySendDesignEvent($"Hero:Crime:{factionName}:JailedPeacefully:Bounty", bounty);
        }
        
        void OnCrimePenaltyWentToJailFromCombat(CrimeOwnerTemplate crimeOwner) {
            string factionName = NiceName(crimeOwner.name);
            int bounty = (int) CrimeUtils.Bounty(crimeOwner);
            AnalyticsUtils.TrySendDesignEvent($"Hero:Crime:{factionName}:JailedFromCombat:Bounty", bounty);
        }

        void OnCrimePenaltyPayedBounty(CrimeOwnerTemplate crimeOwner) {
            string factionName = NiceName(crimeOwner.name);
            int bounty = (int) CrimeUtils.Bounty(crimeOwner);
            AnalyticsUtils.TrySendDesignEvent($"Hero:Crime:{factionName}:PaidBounty", bounty);
        }

        void OnCrimeCommitted(CrimeChangeData crimeData) {
            string factionName = NiceName(crimeData.Faction.name);
            string crimeName = NiceName(crimeData.CrimeCommitted.Archetype.CrimeType.ToString());
            AnalyticsUtils.TrySendDesignEvent($"Hero:Crime:{factionName}:Committed:{crimeName}");
        }
    }
}
#endif