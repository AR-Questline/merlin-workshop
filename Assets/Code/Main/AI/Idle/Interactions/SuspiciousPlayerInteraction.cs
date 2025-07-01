using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class SuspiciousPlayerInteraction : INpcInteraction {
        NpcElement _npc;
        bool _started;
        bool _wantsToEnd;
        
        public bool CanBeInterrupted { get; [UnityEngine.Scripting.Preserve] private set; }
        public bool AllowBarks => true;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => 100;
        public int Priority => 3;
        public bool FullyEntered => true;
        public Vector3? GetInteractionPosition(NpcElement npc) {
            return null;
        }

        public Vector3 GetInteractionForward(NpcElement npc) {
            return Hero.Current.Coords - npc.Coords;
        }

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => false;

        public InteractionBookingResult Book(NpcElement npc) => this.BookOneNpc(ref _npc, npc);
        public void Unbook(NpcElement npc) => _npc = null;

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            _started = true;
            if (_wantsToEnd) {
                DelayEnd().Forget();
            }
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) { }
        public event Action OnInternalEnd;
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) {
            return false;
        }

        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) {
            return false;
        }
        
        void End() {
            OnInternalEnd?.Invoke();
        }

        [UnityEngine.Scripting.Preserve]
        public void DelayedExit() {
            if (!_started) {
                _wantsToEnd = true;
                return;
            }

            DelayEnd().Forget();
        }

        async UniTaskVoid DelayEnd() {
            await AsyncUtil.DelayTime(_npc, 2);
            End();
        }
        
        public bool IsStopping(NpcElement npc) => false;
    }
}