using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class AliveNPCTracker : BaseSimpleTracker<AliveNPCTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.AliveNPCTracker;

        LocationReference _npcLocationReference;
        
        public override void InitFromAttachment(AliveNPCTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _npcLocationReference = spec.npcLocation;
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, IAlive.Events.BeforeDeath, this, OnNpcDeath);
        }

        void OnNpcDeath(DamageOutcome outcome) {
            if (outcome.Target is not NpcElement trackedNpc) {
                return;
            }
            
            if (!_npcLocationReference.IsMatching(null, trackedNpc.ParentModel)) {
                return;
            }
            
            ChangeBy(1f);
        }
    }
}