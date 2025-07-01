using Awaken.Utility;
using System;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Duels {
    public partial class SimpleDuelArena : Element<Location>, IRefreshedByAttachment<SimpleDuelArenaAttachment>, IDuelArena {
        public override ushort TypeForSerialization => SavedModels.SimpleDuelArena;

        SimpleDuelArenaAttachment _spec;
        
        public void InitFromAttachment(SimpleDuelArenaAttachment spec, bool isRestored) {
            _spec = spec;
        }
        
        public async UniTask Teleport(DuelistsGroup[] duelistsGroups, bool fadeOutAfterHeroTeleport) {
            int groupCount = duelistsGroups.Length;
            var groupPositions = GetGroupPositions(groupCount);
            UniTask? heroTeleportTask = null;
            for (int i = 0; i < groupCount; i++) {
                int duelistCount = duelistsGroups[i].Duelists.Count;
                var lookQuaternion = groupPositions[i].GetLookRotation();
                var spawnOffsets = groupPositions[i].GetSpawnOffsets(duelistCount);
                for (int j = 0; j < duelistCount; j++) {
                    var teleportDestination = new TeleportDestination {
                        position = Ground.SnapNpcToGround(ParentModel.Coords + spawnOffsets[j]),
                        Rotation = lookQuaternion
                    };
                    switch (duelistsGroups[i].Duelists[j]) {
                        case NpcDuelistElement npcDuelistElement:
                            npcDuelistElement.NpcElement.Movement.Controller.TeleportTo(teleportDestination, TeleportContext.ToDuelArena);
                            npcDuelistElement.ForceIdlePosition(IdlePosition.World(Ground.SnapNpcToGround(teleportDestination.position + Vector3.up)),
                                IdlePosition.World(teleportDestination.Rotation.HasValue 
                                    ? teleportDestination.Rotation.Value * Vector3.forward 
                                    : Vector3.forward));
                            break;
                        case HeroDuelistElement heroDuelistElement:
                            // All NPC teleports need to be registered before Hero is teleported.
                            heroTeleportTask = HeroTeleportDelayed(heroDuelistElement.Hero, teleportDestination, fadeOutAfterHeroTeleport);
                            break;
                    }
                }
            }

            if (heroTeleportTask.HasValue) {
                await heroTeleportTask.Value;
            }
        }

        async UniTask HeroTeleportDelayed(Hero hero, TeleportDestination teleportDestination, bool fadeOutAfterHeroTeleport) {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            hero.TeleportTo(teleportDestination);
            hero.VHeroController.HeroCamera.InstantSnapDialogueCamera();
            if (fadeOutAfterHeroTeleport) {
                World.Services.Get<TransitionService>().TransitionFromBlack(TransitionService.QuickFadeOut).Forget();
            }
        }

        public void Activate() {
            if (_spec.ObjectToActivate != null) {
                _spec.ObjectToActivate.SetActive(true);
            }
        }
        
        public void Deactivate() {
            if (_spec.ObjectToActivate != null) {
                _spec.ObjectToActivate.SetActive(false);
            }
        }

        GroupPosition[] GetGroupPositions(int groupCount) {
            return _spec.PositionType switch {
                PositionType.RandomOnCircle => GetRandomGroupPositionsOnCircle(_spec.DuelistsSpawnRadius, _spec.ArenaRadius, groupCount),
                PositionType.SpecifiedPositions => _spec.SpecificPositions(groupCount),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        static GroupPosition[] GetRandomGroupPositionsOnCircle(float spawnRadius, float arenaRadius, int groupCount) {
            var groupPositions = new GroupPosition[groupCount];
            var pointsOnCircle = GetOffsetPointsOnCircle(arenaRadius, groupCount);
            for (int i = 0; i < groupCount; i++) {
                groupPositions[i] = new GroupPosition {
                    positionOffset = pointsOnCircle[i],
                    spawnRadius = spawnRadius
                };
            }
            return groupPositions;
        }

        static Vector3[] GetOffsetPointsOnCircle(float radius, int numberOfPoints) {
            Vector3[] points = new Vector3[numberOfPoints];
            float angleStep = 360.0f / numberOfPoints;

            for (int i = 0; i < numberOfPoints; i++) {
                float angle = i * angleStep;
                float radian = Mathf.Deg2Rad * angle;
                float x = radius * Mathf.Cos(radian);
                float z = radius * Mathf.Sin(radian);
                points[i] = new Vector3(x, 0, z);
            }

            return points;
        }

        public enum PositionType : byte {
            RandomOnCircle,
            SpecifiedPositions
        }

        [Serializable]
        public struct GroupPosition {
            public Vector3 positionOffset;
            public float spawnRadius;

            public Quaternion GetLookRotation() {
                return Quaternion.LookRotation(-positionOffset);
            }
            
            public Vector3[] GetSpawnOffsets(int duelistsCount) {
                if (duelistsCount == 1) {
                    return new[] { positionOffset };
                }
                var offsets = GetOffsetPointsOnCircle(spawnRadius, duelistsCount);
                for (int i = 0; i < duelistsCount; i++) {
                    offsets[i] += positionOffset;
                }
                return offsets;
            }
        }
    }
}
