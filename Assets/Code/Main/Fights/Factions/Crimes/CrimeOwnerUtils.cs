using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Awaken.Utility.PhysicUtils;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public static class CrimeOwnerUtils {
        public static CrimeOwners GetCurrentCrimeOwnersFor(this ICrimeSource source, CrimeArchetype archetype) {
            if (source == null) {
                return CrimeOwners.None;
            }
            
            // simple crime type none is used as a scan to get region owners. we skip these checks on scanning
            if (archetype.SimpleCrimeType != SimpleCrimeType.None) {
                if (archetype.IsNoCrime) {
                    return CrimeOwners.None;
                }
                CrimeArchetype overrideArchetype = source.OverrideArchetype(archetype);
                if (overrideArchetype == CrimeArchetype.None) {
                    // We have disabled this crime for this source
                    return CrimeOwners.None;
                }
                archetype = overrideArchetype;
                
                // was this specific crime disabled
                if (source.IsNoCrime(archetype)) {
                    return CrimeOwners.None;
                }
            }

            if (source.Faction?.IsHostileTo(Hero.Current.Faction) == true) {
                return CrimeOwners.None;
            }
            
            // Source has a defined owning faction
            if (source.DefaultOwner is { } crimeOwner) {
                // cases such as a bear walking into a faction owned region
                if (crimeOwner.IsAcceptable(archetype)) {
                    return CrimeOwners.None;
                }

                if (GetCrimeOwnersOfRegion(archetype.CrimeType, source.Position, out var owners)) {
                    return owners;
                }

                return new CrimeOwners(crimeOwner);
            }
            
            // Search for regions that define the crime. For crime sources with no owning faction override
            GetCrimeOwnersOfRegion(archetype.CrimeType, source.Position, out var crimeOwners);
            return crimeOwners;
        }

        public static bool HasCurrentCrimeOwnersFor(this ICrimeSource source, CrimeArchetype archetype) {
            if (source == null) {
                return false;
            }

            // simple crime type none is used as a scan to get region owners. we skip these checks on scanning
            if (archetype.SimpleCrimeType != SimpleCrimeType.None) {
                if (archetype.IsNoCrime) {
                    return false;
                }
                CrimeArchetype overrideArchetype = source.OverrideArchetype(archetype);
                if (overrideArchetype == CrimeArchetype.None) {
                    // We have disabled this crime for this source
                    return false;
                }
                archetype = overrideArchetype;

                // was this specific crime disabled
                if (source.IsNoCrime(archetype)) {
                    return false;
                }
            }

            if (source.Faction?.IsHostileTo(Hero.Current.Faction) == true) {
                return false;
            }

            // Source has a defined owning faction
            if (source.DefaultOwner is { } crimeOwner) {
                // cases such as a bear walking into a faction owned region
                if (crimeOwner.IsAcceptable(archetype)) {
                    return false;
                }

                var success = GetCrimeOwnersOfRegion(archetype.CrimeType, source.Position, out var owners);
                var empty = owners.IsEmpty;
                owners.Dispose();
                return !success || !empty;
            }

            // Search for regions that define the crime. For crime sources with no owning faction override
            GetCrimeOwnersOfRegion(archetype.CrimeType, source.Position, out var crimeOwners);
            var isEmpty = crimeOwners.IsEmpty;
            crimeOwners.Dispose();
            return !isEmpty;
        }

        static readonly HashSet<CrimeOwnerTemplate> ReusableTemplates = new();

        public static bool GetCrimeOwnersOfRegion(CrimeType crimeType, Vector3 point, out CrimeOwners owners) {
            int priority = -1000;

            var isSafe = CollectOwnersToCache(crimeType, point, priority);
            
            // Safe region for this crime
            if (isSafe) {
                owners = CrimeOwners.None;
                ReusableTemplates.Clear();
                return true;
            }
            
            // No regions found for crime
            if (ReusableTemplates.Count == 0) {
                owners = CrimeOwners.None;
                ReusableTemplates.Clear();
                return false;
            }
            
            // Found owners for crime
            owners = new CrimeOwners(ReusableTemplates);
            ReusableTemplates.Clear();
            return true;
        }

        public static CrimeOwnerTemplate GetCrimePrimaryOwnerOfRegion(CrimeType crimeType, Vector3 point) {
            int priority = -1000;

            var isSafe = CollectOwnersToCache(crimeType, point, priority);

            if (isSafe || ReusableTemplates.Count == 0) {
                ReusableTemplates.Clear();
                return null;
            }

            var first = ReusableTemplates.FirstNonAlloc();
            ReusableTemplates.Clear();
            return first;
        }

        static bool CollectOwnersToCache(CrimeType crimeType, Vector3 point, int priority) {
            ReusableTemplates.Clear();

            var isSafe = false;
            foreach (var collider in PhysicsQueries.OverlapSphere(point, 0.1f, RenderLayers.Mask.TriggerVolumes, QueryTriggerInteraction.Collide)) {
                if (collider.TryGetComponentInParent(out CrimeRegion region) && region.Enabled && region.IsForCrime(crimeType)) {
                    if (region.RegionPriority > priority) {
                        priority = region.RegionPriority;
                        isSafe = region.IsSafeRegion;
                        ReusableTemplates.Clear();

                        if (!isSafe && region.isActiveAndEnabled) {
                            var regionOwners = region.Owners;
                            for (var i = 0; i < regionOwners.Length; i++) {
                                if (regionOwners[i].ReputationInRange) {
                                    ReusableTemplates.Add(regionOwners[i].OwnerFaction);
                                }
                            }
                        }
                    } else if (region.RegionPriority == priority) {
                        isSafe = isSafe || region.IsSafeRegion;
                        if (!isSafe && region.isActiveAndEnabled) {
                            var regionOwners = region.Owners;
                            for (var i = 0; i < regionOwners.Length; i++) {
                                if (regionOwners[i].ReputationInRange) {
                                    ReusableTemplates.Add(regionOwners[i].OwnerFaction);
                                }
                            }
                        }
                    }
                }
            }

            return isSafe;
        }
    }
}