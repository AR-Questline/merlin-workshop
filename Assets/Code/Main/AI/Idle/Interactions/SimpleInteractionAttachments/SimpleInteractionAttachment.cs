using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions.SimpleInteractionAttachments {
    public abstract class SimpleInteractionAttachment : MonoBehaviour {
        protected bool _started;
        
        public void Started(NpcElement npc) {
            if (_started) {
                return;
            }

            _started = true;
            OnStarted(npc);
        }

        public void Ended(NpcElement npc) {
            if (!_started) {
                return;
            }
            
            OnEnded(npc);
            _started = false;
        }

        void OnDestroy() {
            if (!_started) {
                return;
            }
            
            OnEnded(null);
            _started = false;
        }

        public abstract void OnStarted(NpcElement npc);
        public abstract void OnEnded(NpcElement npc);
    }
}