using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Utils {
    public class FindNpcUtil {
        public static IEnumerable<NpcElement> FindFromCollection(IEnumerable<NpcElement> npcs, ICharacter owner,
            bool antagonistic, bool neutral, bool ally) {
            ICharacter antagonismTo = null;
            if (owner != null) {
                antagonismTo = owner.TryGetElement(out INpcSummon summon) ? summon.Owner : owner;
            }

            foreach (var npc in npcs) {
                if (antagonismTo == null ||
                    IsMatchingAntagonism(npc.AntagonismTo(antagonismTo), antagonistic, neutral, ally)) {
                    yield return npc;
                }
            }
        }

        public static IEnumerable<NpcElement> FindClosestToCrosshair(IEnumerable<NpcElement> npcs, ICharacter owner,
            float range, bool antagonistic, bool neutral, bool ally) {
            ICharacter antagonismTo = null;
            if (owner != null) {
                antagonismTo = owner.TryGetElement(out INpcSummon summon) ? summon.Owner : owner;
            }

            var firePoint = Hero.Current.VHeroController.FirePoint;
            var origin = firePoint.position;
            var direction = firePoint.forward;
            Ray ray = new(origin, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, range, RenderLayers.Mask.AIs)) {
                if (VGUtils.GetModel<IAlive>(hit.collider.gameObject) is NpcElement npc) {
                    if (antagonismTo == null ||
                        IsMatchingAntagonism(npc.AntagonismTo(antagonismTo), antagonistic, neutral, ally)) {
                        yield return npc;
                    }
                }
            }

            foreach (var npc in ClosestNpc(npcs, antagonistic, neutral, ally, ray, antagonismTo)) {
                yield return npc;
            }
        }

        public static IEnumerable<NpcElement> ClosestNpc(IEnumerable<NpcElement> npcs, bool antagonistic, bool neutral,
            bool ally, Ray ray, ICharacter antagonismTo) {
            if (antagonismTo != null) {
                npcs = npcs.Where(npc => IsMatchingAntagonism(npc.AntagonismTo(antagonismTo), antagonistic, neutral, ally));
            }
            return npcs.OrderByDescending(npc => Vector3.Dot(ray.direction, (npc.Coords - ray.origin).normalized));
        }

        public static IEnumerable<NpcElement> NearestNpc(IEnumerable<NpcElement> npcs, bool antagonistic, bool neutral,
            bool ally, Vector3 position, ICharacter antagonismTo) {
            if (antagonismTo != null) {
                npcs = npcs.Where(npc => IsMatchingAntagonism(npc.AntagonismTo(antagonismTo), antagonistic, neutral, ally));
            }
            return npcs.OrderBy(npc => (position - npc.Coords).sqrMagnitude);
        }

        static bool IsMatchingAntagonism(Antagonism antagonism, bool antagonistic, bool neutral, bool ally) {
            return antagonism switch {
                Antagonism.Neutral => neutral,
                Antagonism.Friendly => ally,
                Antagonism.Hostile => antagonistic,
                _ => false
            };
        }
    }
}