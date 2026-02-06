using Awaken.TG.Main.AI;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using FMODUnity;

namespace Awaken.TG {
    [SpawnsView(typeof(VPlaySfxWhenInIdle))]
    public partial class PlaySfxWhenNpcInIdle : Element<Location>, IRefreshedByAttachment<PlaySfxWhenNpcInIdleAttachment> {
        public override ushort TypeForSerialization => SavedModels.PlaySfxWhenNpcInIdle;
        
        public EventReference sfxToPlay;
        IEventListener _npcDiscardedListener;
        IEventListener _npcStateChangedListener;
        IEventListener _npcIsInDialogueChangedListener;

        VPlaySfxWhenInIdle PlaySfxView => View<VPlaySfxWhenInIdle>();
        
        public void InitFromAttachment(PlaySfxWhenNpcInIdleAttachment spec, bool isRestored) {
            sfxToPlay = spec.sfxToPlay;
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(() => {
                if (ParentModel.TryGetElement(out NpcElement npc)) {
                    OnNpcAttached(npc);
                    return;
                }

                NpcPresence npcPresence = ParentModel.TryGetElement<NpcPresence>();
                if (npcPresence == null) {
                    return;
                }
                if (npcPresence.AliveNpc != null) {
                    OnNpcAttached(npcPresence.AliveNpc);
                }
                npcPresence.ListenTo(NpcPresence.Events.AttachedNpc, OnNpcAttached, this);
            } ,this);
        }

        void OnNpcAttached(NpcElement npcElement) {
            World.EventSystem.TryDisposeListener(ref _npcDiscardedListener);
            World.EventSystem.TryDisposeListener(ref _npcStateChangedListener);
            World.EventSystem.TryDisposeListener(ref _npcIsInDialogueChangedListener);
            npcElement.OnCompletelyInitialized(npc => {
                _npcDiscardedListener = npc.ListenTo(Events.BeforeDiscarded, Discard, this);
                _npcStateChangedListener = npc.ListenTo(NpcAI.Events.NpcStateChanged, _ => OnNpcStateChanged(npc), this);
                _npcIsInDialogueChangedListener = npc.ListenTo(NpcElement.Events.NpcIsInDialogueChanged, _ => OnNpcStateChanged(npc), this);
                OnNpcStateChanged(npc);
            });
        }

        void OnNpcStateChanged(NpcElement npc) {
            if (npc.NpcAI.InIdle && !npc.NpcAI.InCombat && !npc.IsInDialogue) {
                PlaySfxView.Play();
            } else {
                PlaySfxView.Stop();
            }
        }
    }
}
