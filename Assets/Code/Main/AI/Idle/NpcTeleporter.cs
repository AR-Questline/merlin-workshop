using System.Runtime.CompilerServices;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle {
    public static class NpcTeleporter {
        public readonly struct Config {
            public readonly NpcElement npc;
            public readonly Vector3 targetPosition;
            public readonly Hero hero;
            public readonly GameConstants constants;

            public Config(NpcElement npc, Vector3 position) {
                this.npc = npc;
                this.targetPosition = position;
                hero = Hero.Current;
                constants = World.Services.Get<GameConstants>();
            }
        }

        public static bool TryToTeleport(NpcElement npc, Vector3 position, TeleportContext context = TeleportContext.None) {
            return TryToTeleport(new Config(npc, position), context);
        }
        static bool TryToTeleport(in Config config, TeleportContext context = TeleportContext.None) {
            bool inAbyss = NpcPresence.InAbyss(config.npc.Coords);
            if (inAbyss || HeroAllowsTeleporting(config.hero) || !ForbidTeleporting(config)) {
                Teleport(config, context);
                return true;
            }
            return false;
        }

        public static void Teleport(NpcElement npc, Vector3 position, TeleportContext context = TeleportContext.None) {
            Teleport(new Config(npc, position), context);
        }
        static void Teleport(in Config config, TeleportContext context = TeleportContext.None) {
            if (config.npc.IsUnique && NpcPresence.InAbyss(config.npc.Coords) && context != TeleportContext.PresenceRefresh && config.npc.NpcPresence == null) {
                Log.Critical?.Error($"Trying to teleport npc {config.npc} from Abyss! Context: {context}");
            }
            var controller = config.npc.Movement?.Controller;
            if (controller == null) {
                Log.Important?.Error($"Can't teleport npc: {config.npc}, it has no controller.");
                return;
            }
            if (controller.isActiveAndEnabled) {
                controller.TeleportTo(new TeleportDestination { position = config.targetPosition }, context);
            } else {
                controller.DisableFallDamageForTeleport();
                config.npc.ParentModel.SafelyMoveTo(config.targetPosition, true);
                controller.RichAI.ForceTeleport(config.targetPosition);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HeroAllowsTeleporting(Hero hero) {
            return hero == null || hero.IsPortaling || hero.AllowNpcTeleport || World.HasAny<LoadingScreenUI>();
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ForbidTeleporting(in Config config) {
            return IsHeroTooCloseNpc(config) || IsHeroTooCloseTarget(config);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static bool IsNpcCloseEnoughToTarget(in Config config) {
            return Vector3.SqrMagnitude(config.npc.Coords - config.targetPosition) < config.constants.maxNpcToTargetDistanceSqr;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHeroTooCloseNpc(in Config config) {
            return LocationCullingGroup.InActiveLogicBands(config.npc.CurrentDistanceBand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHeroTooCloseTarget(in Config config) {
            return Vector3.SqrMagnitude(config.hero.Coords - config.targetPosition) < config.constants.heroToCloseDistanceSqr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static bool CanHeroSeeNpc(in Config config) {
            return EyesightRaycast(config.hero.Coords + Vector3.up, config.npc.Coords + Vector3.up, config.constants.obstaclesMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static bool CanHeroSeeTarget(in Config config) {
            return EyesightRaycast(config.hero.Coords + Vector3.up, config.targetPosition + Vector3.up, config.constants.obstaclesMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EyesightRaycast(Vector3 origin, Vector3 target, LayerMask obstaclesMask) {
            return !Physics.Raycast(origin, target - origin, (origin - target).magnitude, obstaclesMask,
                QueryTriggerInteraction.Ignore);
        }
    }
    
    public enum TeleportContext : byte {
        None,
        PresenceRefresh,
        Interaction,
        SnapToPositionAndRotate,
        FromCombat,
        MinYCheck,
        AllyTooFar,
        SummonAfterFastTravel,
        ToDuelArena,
        FromStory,
    }
}