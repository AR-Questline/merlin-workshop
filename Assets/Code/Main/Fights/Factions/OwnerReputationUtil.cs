using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Factions.FactionEffects;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Fights.Factions {
    public static class OwnerReputationUtil {
        const string MemoryContext = Faction.FactionContext + ".reputation";
        const string FameContext = "Fame";
        const string InfamyContext = "Infamy";

        static string NoneEffects => LocTerms.None.Translate();
        
        static ContextualFacts FactionsFacts => World.Services.Get<GameplayMemory>().Context(MemoryContext);
        [UnityEngine.Scripting.Preserve] static FactionService FactionService => World.Services.Get<FactionService>();
        static FactionProvider FactionProvider => World.Services.Get<FactionProvider>();
        
        public static string FameKeyReputation(string templateGUID) => $"{templateGUID}_{FameContext}";
        public static string FameKeyPoints(string templateGUID) => $"{templateGUID}_{FameContext}Points";
        public static string InfamyKeyReputation(string templateGUID, bool points = false) => $"{templateGUID}_{InfamyContext}{(points ? "Points" : "")}";
        public static string InfamyKeyPoints(string templateGUID, bool points = false) => $"{templateGUID}_{InfamyContext}{(points ? "Points" : "")}";

        [UnityEngine.Scripting.Preserve]
        public static bool HasReputation(CrimeOwnerTemplate template) => template.hasReputation;
        [UnityEngine.Scripting.Preserve] public static int MaxReputation(CrimeOwnerTemplate template) => template.MaxReputation;
        
        public static int CurrentFamePoints(string templateGUID) => FactionsFacts.Get(FameKeyPoints(templateGUID), 0);
        public static int CurrentInfamyPoints(string templateGUID) => FactionsFacts.Get(InfamyKeyPoints(templateGUID), 0);
        
        public static int CurrentReputation(string fameKeyReputation, string infamyKeyReputation) {
            return FactionsFacts.Get(fameKeyReputation, 0) - FactionsFacts.Get(infamyKeyReputation, 0);
        }

        public static (int infamy, int fame) CurrentReputations(string templateGUID) {
            return (FactionsFacts.Get(InfamyKeyReputation(templateGUID), 0), FactionsFacts.Get(FameKeyReputation(templateGUID), 0));
        }

        public static void ChangeReputation(CrimeOwnerTemplate template, int change, ReputationType type) {
            ReputationKind previousReputationKind = GetCurrentReputationInfo(template).reputationKind;
            int previousReputationPoints = GetReputationPoints(template.GUID, type);
            int newReputationPoints = previousReputationPoints + change;
            newReputationPoints = Mathf.Clamp(newReputationPoints, 0, template.MaxReputation);

            if (newReputationPoints == previousReputationPoints) {
                return;
            }
            
            SetReputationPoints(template, type, newReputationPoints);
            ReputationKind newReputationKind = GetCurrentReputationInfo(template).reputationKind;

            if (newReputationKind != previousReputationKind) {
                var factionReputationBookmark = template.reputationBookmark;
                if (factionReputationBookmark is {IsValid: true}) {
                    Story.StartStory(StoryConfig.Base(factionReputationBookmark, null));
                }
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ReputationInfo GetCurrentReputationInfo(NpcElement npc) {
            using var currentCrimeOwnersFor = npc.GetCurrentCrimeOwnersFor(CrimeArchetype.None);
            if (currentCrimeOwnersFor.IsEmpty) {
                return default;
            }
            return GetCurrentReputationInfo(currentCrimeOwnersFor.PrimaryOwner);
        }
        
        public static ReputationInfo GetCurrentReputationInfo(CrimeOwnerTemplate template) {
            (int infamyIndex, int fameIndex) = GetCurrentReputationIndexes(template);
            return FactionProvider.ReputationMatrix[infamyIndex, fameIndex];
        }
        
        static (int infamyIndex, int fameIndex) GetCurrentReputationIndexes(CrimeOwnerTemplate template) {
            int fameIndex = -1;
            int infamyIndex = -1;
            int famePoints = GetReputationPoints(template.GUID, ReputationType.Fame);
            int infamyPoints = GetReputationPoints(template.GUID, ReputationType.Infamy);

            for (int index = 0; index < template.reputationRanges.Length; index++) {
                IntRange intRange = template.reputationRanges[index];
                
                if (intRange.Contains(famePoints)) {
                    fameIndex = index;
                }

                if (intRange.Contains(infamyPoints)) {
                    infamyIndex = index;
                }
            }

            if (fameIndex == -1 || infamyIndex == -1) {
                Log.Important?.Error($"Couldn't find reputation info for faction {template.name} with fame {famePoints} and infamy {infamyPoints}. Check reputation ranges.");
                fameIndex = 0;
                infamyIndex = 0;
            }

            return (infamyIndex, fameIndex);
        }
        
        public static int GetReputationPoints(string templateGUID, ReputationType type) {
            return type == ReputationType.Fame ? CurrentFamePoints(templateGUID) : CurrentInfamyPoints(templateGUID);
        }
        
        public static void SetReputationPoints(CrimeOwnerTemplate template, ReputationType type, int newReputation) {
            if (type == ReputationType.Fame) {
                FactionsFacts.Set(FameKeyPoints(template.GUID), newReputation);
            } else {
                FactionsFacts.Set(InfamyKeyPoints(template.GUID), newReputation);
            }

            (int infamyIndex, int fameIndex) = GetCurrentReputationIndexes(template);
            FactionsFacts.Set(FameKeyReputation(template.GUID), fameIndex);
            FactionsFacts.Set(InfamyKeyReputation(template.GUID), infamyIndex);
        }

        [UnityEngine.Scripting.Preserve]
        public static string GetFactionEffectsDescription(CrimeOwnerTemplate template, ReputationKind reputationKind) {
            if (template.factionEffects.IsNullOrEmpty()) {
                return NoneEffects;
            }
            
            var factionEffectsBuilder = new StringBuilder();
            foreach (var factionEffect in template.factionEffects) {
                if (!factionEffect.reputationKind.HasFlagFast(reputationKind)) {
                    continue;
                }

                factionEffectsBuilder.AppendLine(factionEffect.effectDescription);
            }
            
            string factionEffects = factionEffectsBuilder.ToString().Trim();

            return string.IsNullOrEmpty(factionEffects)
                ? NoneEffects
                : factionEffects;
        }

        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<FactionEffect> GetAvailableFactionEffects(CrimeOwnerTemplate template) {
            ReputationKind currentReputationKind = GetCurrentReputationInfo(template).reputationKind;
            
            if (currentReputationKind == ReputationKind.None) {
                return Enumerable.Empty<FactionEffect>();
            }
            
            var availableFactionEffects =
                template.factionEffects.Where(fe => fe.reputationKind.HasFlagFast(currentReputationKind));
            return availableFactionEffects;
        }
    }

    public enum ReputationType {
        Infamy = -1,
        Fame = 1,
    }
}