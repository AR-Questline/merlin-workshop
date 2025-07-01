using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Unity.Entities;

namespace Awaken.TG.Main.Fights.Factions {
    public static class WithFactionUtils {
        public static Antagonism AntagonismTo(this IWithFaction me, IWithFaction other) {
            var antagonismMarkers = me.Elements<AntagonismMarker>();
            foreach (AntagonismMarker marker in antagonismMarkers) {
                if (marker.TryGetAntagonismTo(other, out var antagonism)) {
                    return antagonism;
                }
            }

            var otherAntagonismMarkers = other.Elements<AntagonismMarker>();
            foreach (AntagonismMarker marker in otherAntagonismMarkers) {
                if (marker.TryGetAntagonismsFrom(me, out var antagonism)) {
                    return antagonism;
                }
            }
            
            return me.Faction.AntagonismTo(other.Faction);
        }

        /// <summary>
        /// me == other => true
        /// </summary>
        public static bool IsFriendlyTo(this IWithFaction me, IWithFaction other) => me == other || me.AntagonismTo(other) == Antagonism.Friendly;
        public static bool IsNeutralTo(this IWithFaction me, IWithFaction other) => me != other && me.AntagonismTo(other) == Antagonism.Neutral;
        public static bool IsHostileTo(this IWithFaction me, IWithFaction other) => me != other && me.AntagonismTo(other) == Antagonism.Hostile;
        [UnityEngine.Scripting.Preserve]
        public static bool IsFriendlyTo(this IAIEntity me, IWithFaction other) => me.WithFaction.IsFriendlyTo(other);
        [UnityEngine.Scripting.Preserve] 
        public static bool IsNeutralTo(this IAIEntity me, IWithFaction other) => me.WithFaction.IsNeutralTo(other);
        public static bool IsHostileTo(this IAIEntity me, IWithFaction other) => me.WithFaction.IsHostileTo(other);

        public static bool WantToFight(this NpcElement npc, ICharacter target) {
            if (npc.IsHostileTo(target)) {
                return true;
            }
            
            if (npc.IsHeroSummon) {
                return target.IsHostileTo(Hero.Current);
            }

            return target is Hero && CrimeReactionUtils.ShouldReact(npc);
        }
        
        public static void TurnFriendlyTo(this IWithFaction me, AntagonismLayer layer, ICharacter other) => me.OverrideAntagonismTo(other, layer, Antagonism.Friendly);
        
        [UnityEngine.Scripting.Preserve]
        public static void TurnNeutralTo(this IWithFaction me, AntagonismLayer layer, ICharacter other) => me.OverrideAntagonismTo(other, layer, Antagonism.Neutral);
        public static void TurnHostileTo(this IWithFaction me, AntagonismLayer layer, ICharacter other) => me.OverrideAntagonismTo(other, layer, Antagonism.Hostile);

        public static void ClearAllMarkers(this ICharacter me, AntagonismLayer layer) {
            var myMarkers = me.Elements<AntagonismMarker>();
            foreach (var marker in myMarkers.Reverse()) {
                if (marker.Layer == layer) {
                    marker.Discard();
                }
            }
        } 
        
        public static void ClearAllFriendshipWith(this ICharacter me, AntagonismLayer layer, ICharacter other) {
            ClearAllAntagonismWith(me, layer, Antagonism.Friendly, other);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void ClearAllNeutralityWith(this ICharacter me, AntagonismLayer layer, ICharacter other) {
            ClearAllAntagonismWith(me, layer, Antagonism.Neutral, other);
        }
        
        public static void ClearAllHostilityWith(this ICharacter me, AntagonismLayer layer, ICharacter other) {
            ClearAllAntagonismWith(me, layer, Antagonism.Hostile, other);
        }
        
        static void ClearAllAntagonismWith(this ICharacter me, AntagonismLayer layer, Antagonism targetAntagonism, ICharacter other) {
            foreach (var myMarker in me.Elements<AntagonismMarker>().Reverse()) {
                if (myMarker.TryGetAntagonismTo(other, out var antagonism) && antagonism == targetAntagonism && layer >= myMarker.Layer) {
                    myMarker.Discard();
                }
            }

            foreach (var otherMarker in other.Elements<AntagonismMarker>().Reverse()) {
                if (otherMarker.TryGetAntagonismsFrom(me, out var antagonism) && antagonism == targetAntagonism && layer >= otherMarker.Layer) {
                    otherMarker.Discard();
                }
            }
        }

        static void OverrideAntagonismTo(this IWithFaction me, ICharacter other, AntagonismLayer layer, Antagonism antagonism) {
            bool needNewMarker = me.Faction.AntagonismTo(other.Faction) != antagonism;

            foreach (var marker in me.Elements<AntagonismMarker>().Reverse()) {
                if (marker.TryGetAntagonismTo(other, out var markedAntagonism)){
                    if (markedAntagonism == antagonism) {
                        if (marker.Layer == layer && !marker.HasElement<AntagonismDuration>()) {
                            needNewMarker = false;
                        }
                    } else if (layer >= marker.Layer) {
                        marker.Discard();
                    }
                }
            }

            if (needNewMarker) {
                me.AddElement(new CharacterAntagonism(layer, AntagonismType.Mutual, other, antagonism));
            }
        }
    }
}