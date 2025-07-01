using System.Collections.Generic;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories {
    public partial class StoryBasedNpcPresences : Element<Story> {
        readonly Dictionary<LocationTemplate, TemporaryPresenceData> _previousPresencesData = new();

        public void AddPresence(NpcPresence temporaryPresence, bool teleportTo, bool teleportOut) {
            var template = temporaryPresence.Template;
            var previousPresence = temporaryPresence.AliveNpc?.NpcPresence;
            
            if (_previousPresencesData.TryGetValue(template, out var cachedPresence)) {
                _previousPresencesData[template] = cachedPresence.UpdatePresence(temporaryPresence, teleportOut);
            } else {
                _previousPresencesData[template] = new TemporaryPresenceData(previousPresence, temporaryPresence, teleportOut);
            }
            
            temporaryPresence.SetManualAvailability(true, teleportTo);
            if (previousPresence is { IsManual: true }) {
                previousPresence.SetManualAvailability(false, teleportTo);
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop) {
                return;
            }
            foreach (var data in _previousPresencesData.Values) {
                ChangeToPreviousPresence(data).Forget();
            }
            _previousPresencesData.Clear();
        }

        async UniTask ChangeToPreviousPresence(TemporaryPresenceData data) {
            var npc = data.temporaryPresence.AliveNpc;
            if (npc != null) {
                await ParentModel.SetupLocation(npc.ParentModel, false, false, true, false);
                if (!data.teleportOut) {
                    npc.Interactor.Stop(InteractionStopReason.ChangeInteraction, false);
                }
            }
            
            if (data.previousPresence is { } presence) {
                if (presence.IsManual) {
                    presence.SetManualAvailability(true, data.teleportOut);
                } else if (data.teleportOut && npc is { HasBeenDiscarded: false, IsAlive: true }) {
                    NpcTeleporter.Teleport(npc, presence.DesiredPosition, TeleportContext.PresenceRefresh);
                }
            }
            
            data.temporaryPresence.SetManualAvailability(false, data.teleportOut);
        }

        public static StoryBasedNpcPresences GetOrCreate(Story api) {
            if (!api.TryGetElement<StoryBasedNpcPresences>(out var presences)) {
                presences = api.AddElement(new StoryBasedNpcPresences());
            }
            return presences;
        }
        
        public struct TemporaryPresenceData {
            [CanBeNull] public readonly NpcPresence previousPresence;
            public bool teleportOut;
            public NpcPresence temporaryPresence;

            public TemporaryPresenceData(NpcPresence previousPresence, NpcPresence temporaryPresence, bool teleportOut) {
                this.previousPresence = previousPresence;
                this.teleportOut = teleportOut;
                this.temporaryPresence = temporaryPresence;
            }
        
            public TemporaryPresenceData UpdatePresence(NpcPresence temporaryPresence, bool teleportOut) {
                this.temporaryPresence = temporaryPresence;
                this.teleportOut = teleportOut;
                return this;
            }
        }
    }
}
