using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Awaken.TG.Main.Stories.Steps {
    public abstract partial class SNpcTurnHostileBase : StoryStepWithLocationRequirement {
        
        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcTurnHostileBase;

            // Targets
            [Saved] LocationReference _targetsNpcs;
            [Saved] bool _targetsHero;
            [Saved] HostilityData _targetsHostilityData;
            // Npc to change
            [Saved] bool _startFight;
            [Saved] HostilityData _hostilityData;

            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public static StepExecution HostileToHero(bool startFight, HostilityData hostilityData) {
                return new StepExecution(startFight, hostilityData, null, true, HostilityData.Default);
            } 

            public StepExecution(bool startFight, HostilityData hostilityData, [CanBeNull] LocationReference targetsNpcs, bool targetsHero, HostilityData targetsHostilityData) {
                _startFight = startFight;
                _hostilityData = hostilityData;
                
                _targetsNpcs = targetsNpcs;
                _targetsHero = targetsHero;
                _targetsHostilityData = targetsHostilityData;
            }
            
            public override void Execute(Location location) {
                var targets = GetTargets();
                foreach (var target in targets) {
                    if (target is NpcElement npcTarget) {
                        ModifyLocation(npcTarget.ParentModel, _targetsHostilityData);
                    }
                }

                NpcElement npc = location.GetNpcFromLocation();
                if (npc != null) {
                    ModifyLocation(npc.ParentModel, _hostilityData);
                    foreach (var target in targets) {
                        npc.TurnHostileTo(AntagonismLayer.Story, target);
                        if (_startFight) {
                            if (CrimeReactionUtils.IsFleeing(npc)) {
                                npc.Trigger(NpcDangerTracker.Events.CharacterDangerNearby, new NpcDangerTracker.DirectDangerData(npc, target));
                            } else {
                                npc.NpcAI.EnterCombatWith(target);
                            }
                        }
                    }
                }
            }

            List<ICharacter> GetTargets() {
                var targets = new List<ICharacter>();
                if (_targetsNpcs != null) {
                    var targetLocations = _targetsNpcs.MatchingLocations(null);
                    targets.AddRange(targetLocations.Select(l => l.GetNpcFromLocation()).Where(npc => npc != null));
                }
                if (_targetsHero) {
                    targets.Add(Hero.Current);
                }
                return targets;
            }

            static void ModifyLocation(Location location, HostilityData data) {
                KillPreventionElement currentElement = location.TryGetElement<KillPreventionElement>();
                if (data.allowDeath && currentElement != null) {
                    location.RemoveElement(currentElement);
                } else if (!data.allowDeath && currentElement == null) {
                    location.AddElement<KillPreventionElement>();
                }
                
                if (data.markDeathAsNonCriminal) {
                    location.AddMarkerElement<NonCriminalDeathMarker>();
                }
                
                if (data.disableCrimes) {
                    location.AddMarkerElement<NoLocationCrimeOverride>();
                }
            }
        }

        [Serializable]
        public partial struct HostilityData {
            public ushort TypeForSerialization => SavedTypes.HostilityData;

            [Saved] public bool allowDeath;
            [Saved] public bool disableCrimes;
            [Saved] public bool markDeathAsNonCriminal;
            
            public static HostilityData Default => new (false, false, false);
            
            public HostilityData(bool allowDeath, bool disableCrimes, bool markDeathAsNonCriminal) {
                this.allowDeath = allowDeath;
                this.disableCrimes = disableCrimes;
                this.markDeathAsNonCriminal = markDeathAsNonCriminal;
            }
        }
    }
}