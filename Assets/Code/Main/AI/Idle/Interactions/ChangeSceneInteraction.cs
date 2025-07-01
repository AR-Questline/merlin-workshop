using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public partial class ChangeSceneInteraction : INpcInteraction {
        public ushort TypeForSerialization => SavedTypes.ChangeSceneInteraction;

        [Saved] public SceneReference Scene { get; private set; }

        bool _movingToAbyss;
        bool _isStopping;

        public ChangeSceneInteraction(SceneReference scene) {
            Scene = scene;
        }

        public bool CanBeInterrupted => true;
        public bool AllowBarks => true;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => null;
        public int Priority => 99;
        public bool FullyEntered => true;

        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        public Vector3 GetInteractionForward(NpcElement npc) => Vector3.zero;
        
        public Vector3 GetPortalPosition(NpcElement npc) {
            var portal = Portal.FindClosest(Scene, npc, searchForNPC: true);
            if (portal == null) {
                Log.Minor?.Warning($"Npc ({npc}) cannot find portal to {Scene.Name}. It will teleport instantly. This behaviour is normal when hero changing scene", npc.ParentModel.Spec);
                return npc.Coords;
            } else {
                return Ground.SnapNpcToGround(portal.ParentModel.Coords);
            }
        }

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => InteractionBookingResult.ProperlyBooked;
        public void Unbook(NpcElement npc) { }

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            NpcHistorian.NotifyInteractions(npc, $"Start ChangeSceneInteraction {Scene.Name}");
            if (NpcPresence.InAbyss(npc.Coords)) {
                return;
            }
            npc.Movement.Controller.MoveToAbyss();
            npc.AddElement<ChangeSceneHideCompassMarker>();
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            NpcHistorian.NotifyInteractions(npc, $"Stop ChangeSceneInteraction {Scene.Name}");
            if (reason is InteractionStopReason.MySceneUnloading or InteractionStopReason.NPCPresenceDisabled) {
                npc.RemoveElementsOfType<ChangeSceneHideCompassMarker>();
                return;
            }
            if (reason == InteractionStopReason.Death) {
                npc.RemoveElementsOfType<ChangeSceneHideCompassMarker>();
                npc.Movement?.Controller.AbortMoveToAbyss();
                return;
            }
            if (npc.HasBeenDiscarded) {
                npc.RemoveElementsOfType<ChangeSceneHideCompassMarker>();
                return;
            }

            _isStopping = true;
            
            Vector3 comebackPosition;
            Portal portal;
            if (npc.NpcPresence != null) {
                comebackPosition = npc.NpcPresence.DesiredPosition;
                portal = Portal.FindClosest(Scene, comebackPosition);
            } else {
                portal = Portal.FindAnyFromScene(Scene);
                comebackPosition = portal != null ? portal.ParentModel.Coords : Vector3.zero;
            }
            Vector3 position;
            Quaternion rotation;
            int bandOverride;
            float delay;
            if (portal == null) {
                Log.Minor?.Warning($"Npc ({npc}) cannot find portal from {Scene.Name}. It will teleport instantly. This behaviour is normal when hero changing scene", npc.ParentModel.Spec);
                position = comebackPosition;
                rotation = Quaternion.identity;
                bandOverride = -1;
                delay = 0f;
            } else {
                position = portal.ParentModel.Coords;
                rotation = Quaternion.LookRotation(portal.ParentModel.Forward(), Vector3.up);
                bandOverride = portal.ParentModel.GetCurrentBandSafe(-1);
                delay = portal.GetExitDelayForNPC();
            }
            
            position = Ground.SnapNpcToGround(position);
            if (delay > 0) {
                DelayTeleportToPortal(portal, npc, position, rotation, bandOverride, delay).Forget();
            } else {
                TeleportToPortal(npc, position, rotation, bandOverride);
            }
        }
        
        async UniTaskVoid DelayTeleportToPortal(Portal portal, NpcElement npc, Vector3 position, Quaternion rotation, int bandOverride, float delay) {
            if (!await AsyncUtil.DelayTime(portal, delay)) {
                _isStopping = false;
                return;
            }
            TeleportToPortal(npc, position, rotation, bandOverride);
        }

        void TeleportToPortal(NpcElement npc, Vector3 position, Quaternion rotation, int bandOverride) {
            npc.Movement?.Controller.DisableFallDamageForTeleport();
            npc.Controller.RichAI.Teleport(position, true);
            npc.ParentModel.SafelyMoveAndRotateTo(position, rotation, true);
            if (bandOverride != -1) {
                npc.SetTemporaryDistanceBand(bandOverride, 5);
            }
            npc.Movement?.Controller.SetRotationInstant(rotation);
            npc.ParentModel.SetInteractability(LocationInteractability.Active);
            npc.Movement?.Controller.AbortMoveToAbyss();
            npc.RemoveElementsOfType<ChangeSceneHideCompassMarker>();
            _isStopping = false;
        }

        public event Action OnInternalEnd { add { } remove { } }
        
        public bool IsStopping(NpcElement npc) => _isStopping;
        
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}