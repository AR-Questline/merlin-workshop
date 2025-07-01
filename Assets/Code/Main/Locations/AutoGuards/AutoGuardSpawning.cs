using Awaken.Utility;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC.Elements;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Locations.AutoGuards {
    public partial class AutoGuardSpawning : Element<Location>, IRefreshedByAttachment<AutoGuardSpawningAttachment>, IWithFaction {
        public override ushort TypeForSerialization => SavedModels.AutoGuardSpawning;

        const float VeryCloseDistance = 10f;
        const float CloseDistance = 20f;
        const float MidDistance = 30f;
        const float FarDistance = 40f;
        const float MaxAngle = 70f;
        
        Vector3[] _spawnPoints;
        AutoGuardSpawningAttachment _attachment;
        FactionContainer _factionContainer = new();
        
        static readonly NNConstraint Constraint = new() {
            constrainWalkability = true,
            constrainDistance = true,
        };

        public Faction Faction => _factionContainer.Faction;
        public FactionTemplate GetFactionTemplateForSummon() => _factionContainer.GetFactionTemplateForSummon();
        public LocationTemplate RandomGuardTemplate => RandomUtil.UniformSelect(_attachment.GuardTemplates.ToList());

        public void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.OverrideFaction(faction, context);
        public void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.ResetFactionOverride(context);

        public void InitFromAttachment(AutoGuardSpawningAttachment spec, bool isRestored) {
            _attachment = spec;
            _spawnPoints = spec.SpawnPoints.ToArray();
            _factionContainer.SetDefaultFaction(spec.FactionTemplate);
        }
        
        public Vector3? GetSpawnPoint() {
            Vector3? bestPoint = null;
            const float BestScore = -1f;
            
            foreach (Vector3 point in _spawnPoints) {
                var nearestNode = AstarPath.active.GetNearest(point, Constraint);
                if (nearestNode.node == null) {
                    continue;
                }
                Vector3 pointOnNavMesh = nearestNode.position;
                float score = CalculatePointScore(pointOnNavMesh);
                if (score > BestScore) {
                    bestPoint = pointOnNavMesh;
                }
            }

            // Might be null if all points have score -1
            return bestPoint;
        }

        public static float CalculatePointScore(Vector3 point) {
            Vector3 heroPos = Hero.Current.Coords;
            var obstaclesMask = Services.Get<GameConstants>().obstaclesMask;
            float distance = Vector3.Distance(heroPos, point);

            if (distance > FarDistance) {
                return -1f;
            }
            
            bool isHidden = !NpcTeleporter.EyesightRaycast(heroPos + Vector3.up, point + Vector3.up, obstaclesMask);
            bool isInSightClose = AIUtils.IsInCone(heroPos, point, Hero.Current.Forward(), CloseDistance, MaxAngle);
            bool isInSightFar = AIUtils.IsInCone(heroPos, point, Hero.Current.Forward(), FarDistance, MaxAngle);
            
            float distanceScore = distance < VeryCloseDistance ? 1f : Mathf.InverseLerp(MidDistance, VeryCloseDistance, distance);
            
            if (!isHidden && isInSightClose) {
                // Hero is looking at it
                return -1f;
            } else if (!isHidden && isInSightFar) {
                // Hero is seeing it far away
                return 0f;
            } else if (isInSightClose) {
                // Is Hidden and close
                return 0.1f + distanceScore;
            } else if (isInSightFar) {
                // Is Hidden and far
                return 0.02f + distanceScore;
            } else if (!isHidden) {
                // Is Not Hidden but not In Sight (f.e. behind hero)
                return 1f - distanceScore;
            } else {
                // Is Hidden and not In Sight
                return 0.2f + distanceScore;
            }
        }
    }
}