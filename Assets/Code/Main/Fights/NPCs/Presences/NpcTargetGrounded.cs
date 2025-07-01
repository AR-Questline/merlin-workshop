using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Fights.NPCs.Presences {
    public partial class NpcTargetGrounded : Element<NpcElement>, IGrounded {
        static readonly UniversalProfilerMarker NearestToHeroMarker = new("NpcTargetGrounded.NearestToHero");
        static readonly UniversalProfilerMarker NearestToHeroWorldAllMarker = new("NpcTargetGrounded.WorldAll");

        public sealed override bool IsNotSaved => true;

        Vector3 _npcCoords;
        Vector3 _presenceCoords;
        bool _inAbyss;
        
        public Vector3 Coords { get; private set; }
        public Quaternion Rotation => Quaternion.identity;

        protected override void OnInitialize() {
            var npc = ParentModel;
            npc.ListenTo(GroundedEvents.AfterMovedToPosition, AfterNpcMoved, this);
            npc.ListenTo(GroundedEvents.AfterTeleported, _ => this.Trigger(GroundedEvents.AfterTeleported, this), this);
            npc.ListenTo(GroundedEvents.BeforeTeleported, _ => this.Trigger(GroundedEvents.BeforeTeleported, this), this);
            npc.ListenTo(GroundedEvents.TeleportRequested, _ => this.Trigger(GroundedEvents.TeleportRequested, this), this);
            npc.ListenTo(NpcElement.Events.PresenceChanged, NpcPresenceChanged, this);
            AfterNpcMoved(npc.Coords);
        }

        void NpcPresenceChanged(NpcPresence presence) {
            _presenceCoords = presence != null ? presence.ParentModel.Coords : Vector3.zero;
            UpdateCoords();
        }

        void AfterNpcMoved(Vector3 coords) {
            _inAbyss = NpcPresence.InAbyss(coords);
            _npcCoords = coords;
            UpdateCoords();
        }

        void UpdateCoords() {
            var coords = _inAbyss ? GetAbyssPosition() : _npcCoords;
            if (Coords != coords) {
                Coords = coords;
                this.Trigger(GroundedEvents.AfterMoved, this);
                this.Trigger(GroundedEvents.AfterMovedToPosition, coords);
            }
        }

        Vector3 GetAbyssPosition() {
            if (ParentModel.Interactor?.CurrentInteraction is ChangeSceneInteraction changeSceneInteraction) {
                var portal = Portal.FindClosestWithFallback(Hero.Current, changeSceneInteraction.Scene);
                if (portal != null) {
                    return portal.ParentModel.Coords;
                }
            }
            return _presenceCoords;
        }

        [UnityEngine.Scripting.Preserve]
        Vector3 NearestToHero() {
            NearestToHeroMarker.Begin();
            var parentCoords = ParentModel.Coords;
            if (!NpcPresence.InAbyss(parentCoords)) {
                NearestToHeroMarker.End();
                return parentCoords;
            }
            if (ParentModel.NpcPresence is { } uniquePresence) {
                NearestToHeroMarker.End();
                return uniquePresence.InteractionSource.Finder.GetDesiredPosition(ParentModel.Behaviours);
            }
            NearestToHeroWorldAllMarker.Begin();
            var behaviours = ParentModel.Behaviours;
            var nearestCoords = parentCoords;
            var nearestDistance = float.MaxValue;
            foreach (var npcUniquePresence in World.All<NpcPresence>()) {
                if (!npcUniquePresence.IsMine(ParentModel) || !npcUniquePresence.Available) {
                    continue;
                }
                var presencePosition = npcUniquePresence.InteractionSource.Finder.GetDesiredPosition(behaviours);
                var distance = (presencePosition-Hero.Current.Coords).sqrMagnitude;
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestCoords = presencePosition;
                }
            }
            NearestToHeroWorldAllMarker.End();
            NearestToHeroMarker.End();
            return nearestCoords;
        }
    }
}
